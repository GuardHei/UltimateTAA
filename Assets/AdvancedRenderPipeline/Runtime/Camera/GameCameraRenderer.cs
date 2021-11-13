using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
#if UNITY_EDITOR
	public class GameCameraRenderer : CameraRenderer {
#else
public sealed class GameCameraRenderer : CameraRenderer {
#endif

		#region Pipeline Callbacks

		public event Action beforeCull;
		public event Action beforeFirstPass;
		public event Action beforeTransparent;
		public event Action beforePostProcess;
		public event Action afterLastPass;
		public event Action afterSubmission;

		#endregion

		internal readonly BufferedRTHandleSystem _historyBuffers = new BufferedRTHandleSystem();

		internal string _rendererDesc;

		#region RT Handles

		internal RTHandle _rawColorTex;
		internal RTHandle _taaColorTex;
		internal RTHandle _prevTaaColorTex;
		internal RTHandle _hdrColorTex;
		internal RTHandle _displayTex;
		internal RTHandle _depthTex;
		internal RTHandle _prevDepthTex;
		internal RTHandle _stencilTex;
		internal RTHandle _prevStencilTex;
		internal RTHandle _velocityTex;
		internal RTHandle _prevVelocityTex;
		internal RTHandle _gbuffer1Tex;
		internal RTHandle _gbuffer2Tex;

		#endregion

		public GameCameraRenderer(Camera camera) : base(camera) {
			cameraType = AdvancedCameraType.Game;
			_rendererDesc = "Render Game (" + camera.name + ")";
			InitBuffers();
		}

		public override void Render(ScriptableRenderContext context) {
			_context = context;
			_cmd = CommandBufferPool.Get(_rendererDesc);

			BeginSample(_rendererDesc);

			ResetBuffers();
			GetBuffers();

			Setup();

			beforeCull?.Invoke();

			Cull();

			beforeFirstPass?.Invoke();

			DrawDepthStencilPrepass();
			DrawShadowPass();
			DrawOpaqueLightingPass();
			DrawSkybox();

			beforeTransparent?.Invoke();

			DrawTransparentLightingPass();

			beforePostProcess?.Invoke();

			DrawPostFXPass();

			afterLastPass?.Invoke();
			
			FinalBlit();

			EndSample(_rendererDesc);

			Submit();

			afterSubmission?.Invoke();

			ReleaseBuffers();

			DisposeCMD();
		}

		public override void Setup() {
			_context.SetupCameraProperties(camera);

			var screenSize = Vector4.one;
			screenSize.x = internalWidth;
			screenSize.y = internalHeight;
			screenSize.z = 1.0f / internalWidth;
			screenSize.w = 1.0f / internalHeight;
			_cmd.SetGlobalVector(ShaderKeywordManager.SCREEN_SIZE, screenSize);
		}

		public void Cull() {
			if (!camera.TryGetCullingParameters(out var cullingParameters)) {
				Debug.Log("Culling Failed for " + _rendererDesc);
				return;
			}

			_cullingResults = _context.Cull(ref cullingParameters);
		}

		public void DrawDepthStencilPrepass() {
			DrawStaticDepthStencilPrepass();
			DrawDynamicDepthStencilPrepass();
		}

		public void DrawStaticDepthStencilPrepass() {
			SetRenderTarget(_velocityTex, _depthTex);
			ClearRenderTarget(false, true);
			
			ExecuteCommand(_cmd);
			
			var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque | SortingCriteria.OptimizeStateChanges | SortingCriteria.QuantizedFrontToBack };
			var drawSettings = new DrawingSettings(ShaderTagManager.DEPTH_STENCIL, sortingSettings) { enableInstancing = settings.enableAutoInstancing };
			var filterSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: RenderLayerManager.STATIC | RenderLayerManager.TERRAIN);
			
			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawDynamicDepthStencilPrepass() {
			var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque | SortingCriteria.OptimizeStateChanges | SortingCriteria.QuantizedFrontToBack };
			var drawSettings = new DrawingSettings(ShaderTagManager.DEPTH_STENCIL, sortingSettings) { enableInstancing = settings.enableAutoInstancing, perObjectData = PerObjectData.MotionVectors };
			var filterSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: RenderLayerManager.All ^ (RenderLayerManager.STATIC | RenderLayerManager.TERRAIN));
			
			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawShadowPass() { }

		public void DrawOpaqueLightingPass() {
			var clearColor = camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.black;

			SetRenderTarget(_rawColorTex, _depthTex);
			ClearRenderTarget(true, false, clearColor);
			
			ExecuteCommand(_cmd);
			
			var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.OptimizeStateChanges };
			var drawSettings = new DrawingSettings(ShaderTagManager.SRP_DEFAULT_UNLIT, sortingSettings) { enableInstancing = settings.enableAutoInstancing };
			var filterSettings = new FilteringSettings(RenderQueueManager.OPAQUE_QUEUE);

			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawTransparentLightingPass() { }

		public void DrawSkybox() {
			_context.DrawSkybox(camera);
		}

		public void DrawPostFXPass() {
			ResolveTAAPass();
			TonemapPass();
		}

		public void ResolveTAAPass() {
			_cmd.Blit(_rawColorTex, _taaColorTex);

			_cmd.Blit(_taaColorTex, _hdrColorTex);
			
			ExecuteCommand();
		}

		public void TonemapPass() {
			_cmd.Blit(_hdrColorTex, _displayTex);
			ExecuteCommand();
		}

		public void FinalBlit() {
			_cmd.Blit(_displayTex, BuiltinRenderTextureType.CameraTarget);

#if UNITY_EDITOR
			RTHandle debugTex;
			if (AdvancedRenderPipeline.settings.enableDebugView && this is not SceneViewCameraRenderer) { // this is ugly but convenient
				switch (AdvancedRenderPipeline.settings.debugOutput) {
					case DebugOutput.Depth:
						debugTex = _depthTex;
						break;
					case DebugOutput.MotionVector:
						debugTex = _velocityTex;
						break;
					default:
						debugTex = _displayTex;
						break;
				}
				
				_cmd.Blit(debugTex, BuiltinRenderTextureType.CameraTarget);
			}
#endif
			
			ExecuteCommand();
		}

		public void InitBuffers() {
			ResetBuffers();
			_historyBuffers.AllocBuffer(ShaderKeywordManager.RAW_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.TAA_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.HDR_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.DISPLAY_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.DEPTH_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.None,
					depthBufferBits: DepthBits.Depth32), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.VELOCITY_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.R16G16_SNorm), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.GBUFFER_1_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.R16G16_SNorm), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.GBUFFER_2_TEXTURE,
				(system, i) => system.Alloc(size => internalRes, colorFormat: GraphicsFormat.R8G8B8A8_UNorm), 1);
		}

		public void ResetBuffers() {
			_historyBuffers.SwapAndSetReferenceSize(outputWidth, outputHeight);
		}

		public void GetBuffers() {
			_rawColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.RAW_COLOR_TEXTURE, 0);
			_taaColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.TAA_COLOR_TEXTURE, 0);
			_prevTaaColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.TAA_COLOR_TEXTURE, 1);
			// if (_prevRawColorTex == null) Debug.Log("Not Allocated Before!");
			_hdrColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.HDR_COLOR_TEXTURE, 0);
			_displayTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DISPLAY_TEXTURE, 0);
			_depthTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DEPTH_TEXTURE, 0);
			_prevDepthTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DEPTH_TEXTURE, 1);
			_velocityTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.VELOCITY_TEXTURE, 0);
			_prevVelocityTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.VELOCITY_TEXTURE, 1);
			_gbuffer1Tex = _historyBuffers.GetFrameRT(ShaderKeywordManager.GBUFFER_1_TEXTURE, 0);
			_gbuffer2Tex = _historyBuffers.GetFrameRT(ShaderKeywordManager.GBUFFER_2_TEXTURE, 0);
		}

		public void ReleaseBuffers() { }

		public override void Dispose() {
			base.Dispose();
			if (_historyBuffers != null) {
				_historyBuffers.ReleaseAll();
				_historyBuffers.Dispose();
			}
		}
	}
}