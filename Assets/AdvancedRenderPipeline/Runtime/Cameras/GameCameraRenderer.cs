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

		protected bool _enableTaa = true;
		protected string _rendererDesc;

		#region RT Handles & Render Target Identifiers

		protected RTHandle _rawColorTex;
		protected RTHandle _colorTex;
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
		protected RTHandle _gbuffer3Tex;
		protected RTHandle _screenSpaceCubemap;
		protected RTHandle _screenSpaceReflection;
		protected RTHandle _prevScreenSpaceReflection;
		protected RTHandle _indirectSpecular;

		protected RenderTargetIdentifier[] _forwardMRTs = new RenderTargetIdentifier[4];

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
			// camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
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
			SetupSkybox();

			beforeFirstPass?.Invoke();
			
			DrawShadowPass();
			DrawDepthPrepass();
			DrawVelocityPass();
			
			SetupLights();
			
			DrawOpaqueLightingPass();
			DrawSkybox();
			
			DrawSpecularLightingPass();
			
			IntegrateOpaqueLightingPass();

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

			var jitterNum = (int)settings.taaSettings.jitterNum;
			var frameNumCycled = _frameNum % jitterNum;
			
			var frameParams = new Vector4(_frameNum, jitterNum,  frameNumCycled, frameNumCycled / (float) jitterNum);
			_cmd.SetGlobalVector(ShaderKeywordManager.FRAME_PARAMS, frameParams);
			
			_currJitter = _jitterPatterns[frameNumCycled];
			_currJitter *= settings.taaSettings.jitterSpread;
			
			var taaJitter = new Vector4(_currJitter.x, _currJitter.y, _currJitter.x / InternalRes.x, _currJitter.y / InternalRes.y);
			_cmd.SetGlobalVector(ShaderKeywordManager.JITTER_PARAMS, taaJitter);

			if (_enableTaa && settings.taaSettings.enabled) {
				ConfigureProjectionMatrix(_currJitter);
			} else camera.ResetProjectionMatrix();

			_context.SetupCameraProperties(camera);

			var transform = camera.transform;
			
			var cameraViewMatrix = camera.worldToCameraMatrix;
			
			_cameraPosWS = transform.position;
			_cameraFwdWS = cameraViewMatrix.GetViewForward();
			_cameraUpWS = cameraViewMatrix.GetViewUp();
			_cameraRightWS = cameraViewMatrix.GetViewRight();

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

			var viewMatrix = camera.worldToCameraMatrix;
			_matrixVP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * viewMatrix;
			_invMatrixVP = _matrixVP.inverse;
			_nonjitteredMatrixVP = GL.GetGPUProjectionMatrix(camera.nonJitteredProjectionMatrix, false) * viewMatrix;
			_invNonJitteredMatrixVP = _nonjitteredMatrixVP.inverse;

			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_MATRIX_I_VP, _invMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_PREV_MATRIX_VP, IsOnFirstFrame ? _nonjitteredMatrixVP : _prevMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_PREV_MATRIX_I_VP, IsOnFirstFrame ? _invNonJitteredMatrixVP : _prevInvMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_MATRIX_NONJITTERED_VP, _nonjitteredMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_MATRIX_NONJITTERED_I_VP, _invNonJitteredMatrixVP);
			
			var farHalfFovTan = _farPlane * _verticalFovTan;
			
			_prevFrustumCornersWS = _frustumCornersWS;

			_frustumCornersWS = new Matrix4x4();
			
			var fwdDir = _cameraFwdWS * _farPlane;
			var upDir = _cameraUpWS * farHalfFovTan;
			var rightDir = _cameraRightWS * farHalfFovTan * _aspect;
			
			var topLeft = fwdDir + upDir - rightDir;
			var topRight = fwdDir + upDir + rightDir * 3f;
			var bottomLeft = fwdDir - upDir * 3f - rightDir;
			// var bottomRight = fwdDir - upDir * 3f + rightDir * 3f;

			var zBufferParams = new float4((_farPlane - _nearPlane) / _nearPlane,  1f, (_farPlane - _nearPlane) / (_nearPlane * _farPlane), 1f / _farPlane);

			_frustumCornersWS.SetRow(0, new float4(topLeft, .0f));
			_frustumCornersWS.SetRow(1, new float4(bottomLeft, .0f));
			_frustumCornersWS.SetRow(2, new float4(topRight, .0f));
			_frustumCornersWS.SetRow(3, zBufferParams);

			_cameraData[0] = new CameraData {
				cameraPosWS = new float4(_cameraPosWS, .0f),
				cameraFwdWS = new float4(_cameraFwdWS, 1.0f),
				screenSize = screenSize,
				frustumCornersWS = _frustumCornersWS,
				prevFrustumCornersWS = _prevFrustumCornersWS,
				_rtHandleProps = rtProps
			};
			
			// Debug.Log(rtProps.rtHandleScale);
			
			_cameraDataBuffer.SetData(_cameraData);
			
			_cmd.SetGlobalConstantBuffer(_cameraDataBuffer, ShaderKeywordManager.CAMERA_DATA, 0, sizeof(CameraData));
			// _cmd.SetGlobalBuffer(ShaderKeywordManager.CAMERA_DATA, _cameraDataBuffer);

			if (IsOnFirstFrame) {
				_cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_16, settings.blueNoise16);
				_cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_64, settings.blueNoise64);
				_cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_256, settings.blueNoise256);
				_cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_512, settings.blueNoise512);
				_cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_1024, settings.blueNoise1024);
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREINTEGRATED_DGF_LUT, settings.iblLut);
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREINTEGRATED_D_LUT, settings.diffuseIBLLut);
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREINTEGRATED_GF_LUT, settings.specularIBLLut);
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

		public void DrawDepthPrepass() {
			DrawOccluderDepthPrepass();
			DrawRestDepthPrepass();
		}

		public void DrawOccluderDepthPrepass() {
			
		}

		public void DrawRestDepthPrepass() {
			SetRenderTarget(_velocityTex, _depthTex);
			ClearRenderTarget(true, true);
			
			ExecuteCommand(_cmd);
			
			var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque | SortingCriteria.OptimizeStateChanges | SortingCriteria.QuantizedFrontToBack };
			var drawSettings = new DrawingSettings(ShaderTagManager.DEPTH, sortingSettings) { enableInstancing = settings.enableAutoInstancing };
			var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
			
			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawVelocityPass() {
			DrawDynamicVelocityPass();
			DrawStaticVelocityPass();
		}

		public void DrawDynamicVelocityPass() {
			// Assume RT is correctly set by the previous step

			var sortSettings = new SortingSettings(camera) { criteria = SortingCriteria.OptimizeStateChanges };
			var drawSettings = new DrawingSettings(ShaderTagManager.MOTION_VECTORS, sortSettings) { enableInstancing = settings.enableMVAutoInstancing, perObjectData = PerObjectData.MotionVectors };
			// drawSettings.SetShaderPassName(0, ShaderTagManager.MOTION_VECTORS);
			var filterSettings = new FilteringSettings(RenderQueueRange.opaque) { excludeMotionVectorObjects = false };
			
			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawStaticVelocityPass() {
			_cmd.FullScreenPass(MaterialManager.CameraMotionMat, MaterialManager.CAMERA_MOTION_VECTORS_PASS);
			ExecuteCommand();
		}

		public void SetupSkybox() {
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
			_forwardMRTs[3] = _gbuffer3Tex;

			SetRenderTarget(_rawColorTex, _forwardMRTs, _depthTex);
			ClearRenderTarget(true, false, clearColor);
			
			ExecuteCommand();
			
			var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.OptimizeStateChanges };
			var drawSettings = new DrawingSettings(ShaderTagManager.OPAQUE_FORWARD, sortingSettings) { enableInstancing = settings.enableAutoInstancing };
			var filterSettings = new FilteringSettings(RenderQueueManager.OPAQUE_QUEUE);

			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}

		public void DrawTransparentLightingPass() {
			
		}

		public void DrawSkybox() {
			_context.DrawSkybox(camera);
		}

		public void DrawSpecularLightingPass() {
			ComputeScreenSpaceReflectionPass();
			ComputeSpecularIBLPass();
		}
		
		public void ComputeScreenSpaceReflectionPass() {
			_cmd.FullScreenPass(_screenSpaceReflection, MaterialManager.IndirectSpecularMat, MaterialManager.SCREEN_SPACE_REFLECTION_PASS);
			ExecuteCommand();
		}

		public void ComputeSpecularIBLPass() {
			_cmd.SetGlobalTexture(ShaderKeywordManager.DEPTH_TEXTURE, _depthTex);
			_cmd.SetGlobalTexture(ShaderKeywordManager.GBUFFER_1_TEXTURE, _gbuffer1Tex);
			_cmd.SetGlobalTexture(ShaderKeywordManager.GBUFFER_2_TEXTURE, _gbuffer2Tex);
			_cmd.SetGlobalTexture(ShaderKeywordManager.GBUFFER_3_TEXTURE, _gbuffer3Tex);
			_cmd.SetGlobalTexture(ShaderKeywordManager.SCREEN_SPACE_REFLECTION, _screenSpaceReflection);
			_cmd.FullScreenPass(_indirectSpecular, MaterialManager.IndirectSpecularMat, MaterialManager.CUBEMAP_REFLECTION_PASS);
			ExecuteCommand();
		}

		public void IntegrateOpaqueLightingPass() {
			_cmd.SetGlobalTexture(ShaderKeywordManager.RAW_COLOR_TEXTURE, _rawColorTex);
			_cmd.SetGlobalTexture(ShaderKeywordManager.INDIRECT_SPECULAR, _indirectSpecular);
			_cmd.FullScreenPass(_colorTex, MaterialManager.IntegrateOpaqueLightingMat, MaterialManager.INTEGRATE_OPAQUE_LIGHTING_PASS);
			ExecuteCommand();
		}

		public void DrawPostFXPass() {
			StopNaNPropagationPass();
			ResolveTAAPass();
			TonemapPass();
		}

		public void StopNaNPropagationPass() {
			if (!settings.stopNaNPropagation) return;
			_cmd.Blit(_colorTex, _taaColorTex, MaterialManager.TonemappingMat, MaterialManager.STOP_NAN_PROPAGATION_PASS);
			_cmd.Blit(_taaColorTex, _colorTex);
			ExecuteCommand();
		}

		public void ResolveTAAPass() {

			if (!IsOnFirstFrame && settings.taaSettings.enabled && _enableTaa) {
				MaterialManager.TaaMat.SetFloat(ShaderKeywordManager.ENABLE_REPROJECTION, 1f);
				MaterialManager.TaaMat.SetVector(ShaderKeywordManager.TAA_PARAMS_0, settings.taaSettings.TaaParams0);
				MaterialManager.TaaMat.SetVector(ShaderKeywordManager.TAA_PARAMS_1, settings.taaSettings.TaaParams1);
				MaterialManager.TaaMat.SetVector(ShaderKeywordManager.TAA_PARAMS_2, settings.taaSettings.TaaParams2);
				
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREV_TAA_COLOR_TEXTURE, _prevTaaColorTex);
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREV_DEPTH_TEXTURE, _prevDepthTex);
				_cmd.SetGlobalTexture(ShaderKeywordManager.STENCIL_TEXTURE, _depthTex, RenderTextureSubElement.Stencil);
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREV_STENCIL_TEXTURE, _prevDepthTex, RenderTextureSubElement.Stencil);
				_cmd.SetGlobalTexture(ShaderKeywordManager.VELOCITY_TEXTURE, _velocityTex);
				_cmd.SetGlobalTexture(ShaderKeywordManager.PREV_VELOCITY_TEXTURE, _prevVelocityTex);
			} else MaterialManager.TaaMat.SetFloat(ShaderKeywordManager.ENABLE_REPROJECTION, -1f);

			_cmd.Blit(_colorTex, _taaColorTex, MaterialManager.TaaMat, MaterialManager.TEMPORAL_ANTI_ALIASING_PASS);
			_cmd.Blit(_taaColorTex, _hdrColorTex, MaterialManager.TonemappingMat, MaterialManager.FAST_INVERT_TONEMAPPING_PASS);

			ExecuteCommand();
		}

		public void TonemapPass() {

			var colorGradeParams = new Vector4(
				Mathf.Pow(2f, settings.colorSettings.postExposure), 
				settings.colorSettings.contrast * .01f + 1f, 
				settings.colorSettings.hueShift * (1f / 360f), 
				settings.colorSettings.saturation * .01f + 1f);
			
			MaterialManager.TonemappingMat.SetInteger(ShaderKeywordManager.TONEMAPPING_MODE, (int) settings.tonemappingSettings.tonemappingMode);
			MaterialManager.TonemappingMat.SetVector(ShaderKeywordManager.COLOR_GRADE_PARAMS, colorGradeParams);
			MaterialManager.TonemappingMat.SetColor(ShaderKeywordManager.COLOR_FILTER, settings.colorSettings.colorFilter.linear);
			_cmd.Blit(_hdrColorTex, _displayTex, MaterialManager.TonemappingMat, MaterialManager.TONEMAPPING_PASS);
			ExecuteCommand();
		}

		public void FinalBlit() {
			RTHandle src = _displayTex;
#if !UNITY_EDITOR
			_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
#else
			if (settings.enableDebugView && (this is not SceneViewCameraRenderer || settings.enableDebugViewInEditor)) { // this is ugly but convenient
				switch (settings.debugOutput) {
					case DebugOutput.Depth:
						src = _depthTex;
						_cmd.BlitDepth(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.Stencil:
						src = _depthTex;
						_cmd.BlitDebugStencil(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.GBuffer1:
						src = _gbuffer1Tex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.GBuffer2:
						src = _gbuffer2Tex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.GBuffer3:
						src = _gbuffer3Tex;
						_cmd.BlitDebugIBLOcclusion(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.Smoothness:
						src = _gbuffer2Tex;
						_cmd.BlitDebugSmoothness(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.Velocity:
						src = _velocityTex;
						// _cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						_cmd.BlitDebugVelocity(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.ScreenSpaceCubemap:
						src = _screenSpaceCubemap;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.ScreenSpaceReflection:
						src = _screenSpaceReflection;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.ScreenSpaceReflectionHistory:
						src = _prevScreenSpaceReflection;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.IndirectSpecular:
						src = _indirectSpecular;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.RawColor:
						src = _rawColorTex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.Color:
						src = _colorTex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.TaaColor:
						src = _taaColorTex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.TaaColorHistory:
						src = _prevTaaColorTex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.HDRColor:
						src = _hdrColorTex;
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
					case DebugOutput.NaN:
						src = _hdrColorTex;
						_cmd.BlitDebugNaN(src, BuiltinRenderTextureType.CameraTarget);
						break;
					default:
						_cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
						break;
				}
			} else _cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
#endif
			ExecuteCommand();
		}

		protected override void UpdateRenderScale(bool outputChanged = true) {
			base.UpdateRenderScale(outputChanged);
			if (outputChanged) {
				ResetFrameHistory();
				ResetBufferSize();
			}
		}

		public void InitBuffers() {
			_historyBuffers.AllocBuffer(ShaderKeywordManager.RAW_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "RawColorTex"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.COLOR_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "ColorTex"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.TAA_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "TaaColorTex "), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.HDR_COLOR_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "HDRColorTex"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.DISPLAY_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
					filterMode: FilterMode.Bilinear, name: "DisplayColorTex"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.DEPTH_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.None,
					depthBufferBits: DepthBits.Depth32, name: "DepthTex "), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.VELOCITY_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16_SNorm, name: "VelocityTex "), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.GBUFFER_1_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16_UNorm, name: "GBuffer1"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.GBUFFER_2_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "GBuffer2"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.GBUFFER_3_TEXTURE,
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R8_UNorm, name: "GBuffer3"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.SCREEN_SPACE_CUBEMAP, 
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, name: "ScreenSpaceCubemap"), 1);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.SCREEN_SPACE_REFLECTION, 
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "ScreenSpaceReflection "), 2);
			_historyBuffers.AllocBuffer(ShaderKeywordManager.INDIRECT_SPECULAR, 
				(system, i) => system.Alloc(size => InternalRes, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, name: "IndirectSpecular"), 1);
		}

		public void ResetBufferSize() {
			// Debug.Log("Reset!");
			// Debug.Log(Time.frameCount + ", " + _frameNum + " " + camera.name + " Reset to " + OutputRes);
			// _historyBuffers.SwapAndSetReferenceSize(OutputRes.x, OutputRes.y);
			_historyBuffers.ResetReferenceSize(OutputRes.x, OutputRes.y);
		}

		public void GetBuffers() {
			
			_historyBuffers.SwapAndSetReferenceSize(OutputRes.x, OutputRes.y);
			
			_rawColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.RAW_COLOR_TEXTURE, 0);
			_colorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.COLOR_TEXTURE, 0);
			_taaColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.TAA_COLOR_TEXTURE, 0);
			_hdrColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.HDR_COLOR_TEXTURE, 0);
			_displayTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DISPLAY_TEXTURE, 0);
			_depthTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DEPTH_TEXTURE, 0);
			_velocityTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.VELOCITY_TEXTURE, 0);
			_gbuffer1Tex = _historyBuffers.GetFrameRT(ShaderKeywordManager.GBUFFER_1_TEXTURE, 0);
			_gbuffer2Tex = _historyBuffers.GetFrameRT(ShaderKeywordManager.GBUFFER_2_TEXTURE, 0);
			_gbuffer3Tex = _historyBuffers.GetFrameRT(ShaderKeywordManager.GBUFFER_3_TEXTURE, 0);
			_screenSpaceCubemap = _historyBuffers.GetFrameRT(ShaderKeywordManager.SCREEN_SPACE_CUBEMAP, 0);
			_screenSpaceReflection = _historyBuffers.GetFrameRT(ShaderKeywordManager.SCREEN_SPACE_REFLECTION, 0);
			_indirectSpecular = _historyBuffers.GetFrameRT(ShaderKeywordManager.INDIRECT_SPECULAR, 0);

			_prevTaaColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.TAA_COLOR_TEXTURE, 1);
			_prevDepthTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DEPTH_TEXTURE, 1);
			_prevVelocityTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.VELOCITY_TEXTURE, 1);
			_prevScreenSpaceReflection = _historyBuffers.GetFrameRT(ShaderKeywordManager.SCREEN_SPACE_REFLECTION, 1);
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
			if (_historyBuffers != null) {
				_historyBuffers.ReleaseAll();
				_historyBuffers.Dispose();
			}
			
			ReleaseComputeBuffers();
			
			base.Dispose();
		}
	}
}