using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.Cameras {
#if UNITY_EDITOR
	public unsafe class GameCameraRenderer : CameraRenderer {
#else
public unsafe class GameCameraRenderer : CameraRenderer {
#endif

		#region Pipeline Callbacks

		public event Action beforeCull;
		public event Action beforeFirstPass;
		public event Action beforeTransparent;
		public event Action beforePostProcess;
		public event Action afterLastPass;
		public event Action afterSubmission;

		#endregion

		protected readonly BufferedRTHandleSystem _historyBuffers = new();

		protected string _rendererDesc;

		#region RT Handles & Render Target Identifiers

		protected RTHandle _rawColorTex;
		protected RTHandle _taaColorTex;
		protected RTHandle _prevTaaColorTex;
		protected RTHandle _hdrColorTex;
		protected RTHandle _displayTex;
		protected RTHandle _depthTex;
		protected RTHandle _prevDepthTex;
		protected RTHandle _stencilTex;
		protected RTHandle _prevStencilTex;
		protected RTHandle _velocityTex;
		protected RTHandle _prevVelocityTex;
		protected RTHandle _gbuffer1Tex;
		protected RTHandle _gbuffer2Tex;

		protected RenderTargetIdentifier[] _forwardMRTs = new RenderTargetIdentifier[3];

		#endregion

		#region Compute Buffers

		protected CameraData[] _cameraData;
		protected DirectionalLight[] _mainLights;

		protected ComputeBuffer _cameraDataBuffer;
		protected ComputeBuffer _mainLightBuffer;

		#endregion

		public GameCameraRenderer(Camera camera) : base(camera) {
			cameraType = AdvancedCameraType.Game;
			_rendererDesc = "Render Game (" + camera.name + ")";
			InitBuffers();
			InitComputeBuffers();
		}

		public override void Render(ScriptableRenderContext context) {
			_context = context;
			_cmd = CommandBufferPool.Get(_rendererDesc);

			BeginSample(_rendererDesc);

			GetBuffers();

			Setup();

			beforeCull?.Invoke();

			Cull();

			beforeFirstPass?.Invoke();
			
			DrawShadowPass();

			DrawDepthStencilPrepass();
			
			SetupLights();
			
			DrawOpaqueLightingPass();
			DrawSkybox();
			
			DrawSpecularLightingPass();

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

			DisposeCommandBuffer();
		}

		public override void Setup() {
			_context.SetupCameraProperties(camera);

			var transform = camera.transform;
			_cameraPosWS = transform.position;
			_cameraFwdWS = transform.forward;

			var screenSize = Vector4.one;
			screenSize.x = InternalRes.x;
			screenSize.y = InternalRes.y;
			screenSize.z = 1.0f / InternalRes.x;
			screenSize.w = 1.0f / InternalRes.y;

			var rtProps = _historyBuffers.rtHandleProperties.Pack();

			/*
			if (IsOnFirstFrame) {
				rtProps.viewportSize.zw = rtProps.viewportSize.xy;
				rtProps.rtSize.zw = rtProps.rtSize.xy;
				rtProps.rtHandleScale.zw = rtProps.rtHandleScale.xy;
			}
			*/

			_cameraData[0] = new CameraData {
				cameraPosWS = _cameraPosWS,
				cameraFwdWS = _cameraFwdWS,
				screenSize = screenSize,
				_rtHandleProps = rtProps
			};
			
			// Debug.Log(rtProps.rtHandleScale);
			
			_cameraDataBuffer.SetData(_cameraData);
			
			_cmd.SetGlobalConstantBuffer(_cameraDataBuffer, ShaderKeywordManager.CAMERA_DATA, 0, sizeof(CameraData));

			ExecuteCommand();
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
		
		public void SetupLights() {
			LightManager.UpdateLight(_cullingResults);
			_mainLights[0] = LightManager.mainLightData;
			_mainLightBuffer.SetData(_mainLights, 0, 0, 1);
			_cmd.SetGlobalConstantBuffer(_mainLightBuffer, ShaderKeywordManager.MAIN_LIGHT_DATA, 0, sizeof(DirectionalLight));
			ExecuteCommand();
		}

		public void DrawShadowPass() {
			
		}

		public void DrawOpaqueLightingPass() {
			var clearColor = camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.black;
			clearColor = Color.clear; // MRT needs to be cleared with 0 anyway

			_forwardMRTs[0] = _rawColorTex;
			_forwardMRTs[1] = _gbuffer1Tex;
			_forwardMRTs[2] = _gbuffer2Tex;

			SetRenderTarget(_rawColorTex, _forwardMRTs, _depthTex);
			ClearRenderTarget(true, false, clearColor);
			
			ExecuteCommand(_cmd);
			
			var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.OptimizeStateChanges };
			var drawSettings = new DrawingSettings(ShaderTagManager.FORWARD, sortingSettings) { enableInstancing = settings.enableAutoInstancing };
			var filterSettings = new FilteringSettings(RenderQueueManager.OPAQUE_QUEUE);

			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawTransparentLightingPass() {
			
		}

		public void DrawSkybox() {
			_context.DrawSkybox(camera);
		}

		public void DrawSpecularLightingPass() {
			ComputeSpecularIBLPass();
			ComputeScreenSpaceReflectionPass();
			IntegrateSpecularLightingPass();
		}

		public void ComputeSpecularIBLPass() {
			
		}

		public void ComputeScreenSpaceReflectionPass() {
			
		}

		public void IntegrateSpecularLightingPass() {
			
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
			RTHandle src = _displayTex;
#if !UNITY_EDITOR
			_cmd.ScaledBlit(src, BuiltinRenderTextureType.CameraTarget);
#else
			if (AdvancedRenderPipeline.settings.enableDebugView && this is not SceneViewCameraRenderer) { // this is ugly but convenient
				switch (AdvancedRenderPipeline.settings.debugOutput) {
					case DebugOutput.Depth:
						src = _depthTex;
						_cmd.BlitDepth(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.GBuffer1:
						src = _gbuffer1Tex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.GBuffer2:
						src = _gbuffer2Tex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.MotionVector:
						src = _velocityTex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					default:
						src = _displayTex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
				}
			} else _cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
#endif
			ExecuteCommand();
		}

		protected override void UpdateRenderScale(bool outputChanged = true) {
			base.UpdateRenderScale(outputChanged);
			if (outputChanged) ResetBufferSize();
		}

		public void InitBuffers() {
			// ResetBufferSize();
			_historyBuffers.AllocBuffer(ShaderKeywordManager.RAW_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "RawColorBuffer"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.TAA_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "TaaColorBuffer"), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.HDR_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "HDRColorBuffer"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.DISPLAY_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "DisplayColorBuffer"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.DEPTH_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.None,
					depthBufferBits: DepthBits.Depth32, name: "DepthBuffer"), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.VELOCITY_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16_SNorm, name: "VelocityBuffer"), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.GBUFFER_1_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16_SNorm, name: "GBuffer1"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.GBUFFER_2_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "GBuffer2"), 1);
		}

		public void ResetBufferSize() {
			// Debug.Log("Reset!");
			// Debug.Log(Time.frameCount + ", " + _frameNum + " " + camera.name + " Reset to " + OutputRes);
			// _historyBuffers.SwapAndSetReferenceSize(OutputRes.x, OutputRes.y);
			_historyBuffers.ResetReferenceSize(OutputRes.x, OutputRes.y);
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
			
			
			// Debug.Log("Raw Color S: " + _rawColorTex.referenceSize);
			// Debug.Log("Velocity S: " + _rawColorTex.referenceSize);
			// Debug.Log("Depth S: " + _depthTex.referenceSize)
		}

		public void ReleaseBuffers() { }

		public void InitComputeBuffers() {
			_cameraData = new CameraData[1];
			_cameraDataBuffer = new ComputeBuffer(1, sizeof(CameraData), ComputeBufferType.Constant);
			
			_mainLights = new DirectionalLight[1];
			_mainLightBuffer = new ComputeBuffer(1, sizeof(DirectionalLight), ComputeBufferType.Constant);
		}

		public void ReleaseComputeBuffers() {
			_cameraDataBuffer.Dispose();
			_mainLightBuffer.Dispose();
		}

		public override void Dispose() {
			base.Dispose();
			if (_historyBuffers != null) {
				_historyBuffers.ReleaseAll();
				_historyBuffers.Dispose();
			}
			
			ReleaseComputeBuffers();
		}
	}
}