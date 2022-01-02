using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.Cameras {
	public unsafe class GameCameraRenderer : CameraRenderer {

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
		protected RTHandle _screenSpaceCubemap;

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

			SetupSkybox();
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
			_cameraUpWS = transform.up;
			_cameraRightWS = transform.right;

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

			var matrixVP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix;
			var invMatrixVP = matrixVP.inverse;
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_MATRIX_I_VP, invMatrixVP);

			var aspect = camera.aspect;
			var nearClip = camera.nearClipPlane;
			var farClip = camera.farClipPlane;
			var farHalfFovTan = farClip * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);

			_frustumCornersWS = new Matrix4x4();
			
			var fwdDir = _cameraFwdWS * farClip;
			var upDir = _cameraUpWS * farHalfFovTan;
			var rightDir = _cameraRightWS * farHalfFovTan * aspect;
			
			var topLeft = fwdDir + upDir - rightDir;
			var topRight = fwdDir + upDir + rightDir * 3f;
			var bottomLeft = fwdDir - upDir * 3f - rightDir;
			// var bottomRight = fwdDir - upDir + dRightDir;

			var zBufferParams = new float4((farClip - nearClip) / nearClip,  1f, (farClip - nearClip) / (nearClip * farClip), 1f / farClip);
			
			_frustumCornersWS.SetRow(0, new float4(topLeft, .0f));
			_frustumCornersWS.SetRow(1, new float4(bottomLeft, .0f));
			_frustumCornersWS.SetRow(2, new float4(topRight, .0f));
			_frustumCornersWS.SetRow(3, zBufferParams);
			
			// Debug.Log(topLeft + " | " + bottomLeft + " | " + bottomRight);

			/*
			var fwdDir = _cameraFwdWS * farClip;
			var upDir = _cameraUpWS * farHalfFovTan;
			var rightDir = _cameraRightWS * farHalfFovTan * aspect;
			
			var topLeft = fwdDir + upDir - rightDir;
			var topRight = fwdDir + upDir + rightDir;
			var bottomRight = fwdDir - upDir + rightDir;
			var bottomLeft = fwdDir - upDir - rightDir;
			
			// align like this to avoid branching on shader side
			_frustumCornersWS.SetRow(0, new float4(topLeft, .0f));
			_frustumCornersWS.SetRow(0, new float4(topRight, .0f));
			_frustumCornersWS.SetRow(0, new float4(bottomRight, .0f));
			_frustumCornersWS.SetRow(0, new float4(bottomLeft, .0f));
			*/

			_cameraData[0] = new CameraData {
				cameraPosWS = new float4(_cameraPosWS, .0f),
				cameraFwdWS = new float4(_cameraFwdWS, 1.0f),
				screenSize = screenSize,
				frustumCornersWS = _frustumCornersWS,
				_rtHandleProps = rtProps
			};
			
			// Debug.Log(rtProps.rtHandleScale);
			
			_cameraDataBuffer.SetData(_cameraData);
			
			_cmd.SetGlobalConstantBuffer(_cameraDataBuffer, ShaderKeywordManager.CAMERA_DATA, 0, sizeof(CameraData));
			// _cmd.SetGlobalBuffer(ShaderKeywordManager.CAMERA_DATA, _cameraDataBuffer);

			if (IsOnFirstFrame) {
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREINTEGRATED_DGF_LUT, settings.iblLut);
				_cmd.SetGlobalTexture(ShaderKeywordManager.GLOBAL_ENV_MAP_SPECULAR, settings.globalEnvMapSpecular);
				_cmd.SetGlobalTexture(ShaderKeywordManager.GLOBAL_ENV_MAP_DIFFUSE, settings.globalEnvMapDiffuse);
			}

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

		public void SetupSkybox() {
			_cmd.SetGlobalFloat(ShaderKeywordManager.GLOBAL_ENV_MAP_EXPOSURE, settings.globalEnvMapExposure);
			_cmd.SetGlobalFloat(ShaderKeywordManager.GLOBAL_ENV_MAP_ROTATION, settings.globalEnvMapRotation);
			_cmd.SetGlobalFloat(ShaderKeywordManager.SKYBOX_MIP_LEVEL, settings.skyboxMipLevel);
		}
		
		public void SetupLights() {
			LightManager.UpdateLight(_cullingResults);
			_mainLights[0] = LightManager.mainLightData;
			_mainLightBuffer.SetData(_mainLights, 0, 0, 1);
			_cmd.SetGlobalConstantBuffer(_mainLightBuffer, ShaderKeywordManager.MAIN_LIGHT_DATA, 0, sizeof(DirectionalLight));
			// _cmd.SetGlobalBuffer(ShaderKeywordManager.MAIN_LIGHT_DATA, _mainLightBuffer);
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
			
			ExecuteCommand();
			
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
			_cmd.SetGlobalTexture(ShaderKeywordManager.DEPTH_TEXTURE, _depthTex);
			_cmd.SetGlobalTexture(ShaderKeywordManager.GBUFFER_1_TEXTURE, _gbuffer1Tex);
			_cmd.SetGlobalTexture(ShaderKeywordManager.GBUFFER_2_TEXTURE, _gbuffer2Tex);
			_cmd.FullScreenPass(_screenSpaceCubemap, MaterialManager.ScreenSpaceCubemapReflectionMaterial, 0);
			ExecuteCommand();
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
			// _cmd.Blit(_hdrColorTex, _displayTex);
			MaterialManager.TonemappingMaterial.SetInteger(ShaderKeywordManager.TONEMAPPING_MODE, (int) settings.tonemappingSettings.tonemappingMode);
			_cmd.Blit(_hdrColorTex, _displayTex, MaterialManager.TonemappingMaterial, 0);
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
					case DebugOutput.ScreenSpaceCubemap:
						src = _screenSpaceCubemap;
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
			_historyBuffers.AllocBuffer(ShaderKeywordManager.SCREEN_SPACE_CUBEMAP, 
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, filterMode: FilterMode.Bilinear, name: "ScreenSpaceCubemap"), 1);
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
			_screenSpaceCubemap = _historyBuffers.GetFrameRT(ShaderKeywordManager.SCREEN_SPACE_CUBEMAP, 0);

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