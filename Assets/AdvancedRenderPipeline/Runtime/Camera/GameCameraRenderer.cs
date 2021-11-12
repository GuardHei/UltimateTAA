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

			EndSample(_rendererDesc);

			Submit();

			afterSubmission?.Invoke();

			ReleaseBuffers();

			CommandBufferPool.Release(_cmd);
		}

		public override void Setup() {
			_context.SetupCameraProperties(camera);

			var clearColor = camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.black;

			SetRenderTarget(_rawColorTex, _depthTex);
			ClearRenderTarget(RTClearFlags.All, clearColor);

			ExecuteCommand(_cmd);
		}

		public void Cull() {
			if (!camera.TryGetCullingParameters(out var cullingParameters)) {
				Debug.Log("Culling Failed for " + _rendererDesc);
				return;
			}

			_cullingResults = _context.Cull(ref cullingParameters);
		}

		public void DrawDepthStencilPrepass() {
			var sortingSettings = new SortingSettings(camera) {
				criteria = SortingCriteria.CommonOpaque | SortingCriteria.OptimizeStateChanges |
				           SortingCriteria.QuantizedFrontToBack
			};
			var drawSettings = new DrawingSettings(ShaderTagManager.DEPTH_STENCIL, sortingSettings) {
				enableInstancing = settings.enableAutoInstancing
			};
			var filterSettings = new FilteringSettings(RenderQueueRange.opaque);

			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawShadowPass() { }

		public void DrawOpaqueLightingPass() {
			var sortingSettings = new SortingSettings(camera) {
				criteria = SortingCriteria.OptimizeStateChanges
			};
			var drawSettings = new DrawingSettings(ShaderTagManager.SRP_DEFAULT_UNLIT, sortingSettings) {
				enableInstancing = settings.enableAutoInstancing
			};
			var filterSettings = new FilteringSettings(RenderQueueManager.OPAQUE_QUEUE);

			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawTransparentLightingPass() { }

		public void DrawSkybox() {
			_context.DrawSkybox(camera);
		}

		public void DrawPostFXPass() {
			ResolveTAAPass();
			FinalBlit();
		}

		public void ResolveTAAPass() {
			_cmd.Blit(_rawColorTex, _taaColorTex);

			_cmd.Blit(_taaColorTex, _hdrColorTex);
		}

		public void FinalBlit() {
			_cmd.Blit(_hdrColorTex, BuiltinRenderTextureType.CameraTarget);
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
		}

		public void ReleaseBuffers() { }

		public override void Dispose() {
			base.Dispose();
			_historyBuffers.ReleaseAll();
			// _historyBuffers.Dispose();
		}
	}
}