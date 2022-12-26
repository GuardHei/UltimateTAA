using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class ShaderKeywordManager {

		#region Camera Params

		public static readonly int CAMERA_DATA = Shader.PropertyToID("CameraData");
		public static readonly int UNITY_MATRIX_VP = Shader.PropertyToID("unity_MatrixVP");
		public static readonly int UNITY_MATRIX_I_VP = Shader.PropertyToID("unity_InvMatrixVP");
		public static readonly int UNITY_PREV_MATRIX_VP = Shader.PropertyToID("unity_MatrixPreviousVP");
		public static readonly int UNITY_PREV_MATRIX_I_VP = Shader.PropertyToID("unity_InvMatrixPreviousVP");
		public static readonly int UNITY_MATRIX_NONJITTERED_VP = Shader.PropertyToID("_NonJitteredMatrixVP");
		public static readonly int UNITY_MATRIX_NONJITTERED_I_VP = Shader.PropertyToID("_InvNonJitteredMatrixVP");

		#endregion

		#region Light Params

		public static readonly int MAIN_LIGHT = Shader.PropertyToID("_MainLight");
		public static readonly int MAIN_LIGHT_DATA = Shader.PropertyToID("MainLightData");

		#endregion

		#region Shadow Params

		public static readonly int MAIN_LIGHT_SHADOW_DATA = Shader.PropertyToID("MainLightShadowData");
		public static readonly int MAIN_LIGHT_SHADOW_PARAMS_0 = Shader.PropertyToID("_MainLightShadowParams0");
		public static readonly int MAIN_LIGHT_SHADOW_PARAMS_1 = Shader.PropertyToID("_MainLightShadowParams1");
		public static readonly int MAIN_LIGHT_SHADOW_PARAMS_2 = Shader.PropertyToID("_MainLightShadowParams2");
		public static readonly int MAIN_LIGHT_SHADOW_BOUND_ARRAY = Shader.PropertyToID("_MainLightShadowBoundArray");
		public static readonly int MAIN_LIGHT_SHADOW_MATRIX_VP_ARRAY = Shader.PropertyToID("_MainLightShadowMatrixVPArray");
		public static readonly int MAIN_LIGHT_SHADOW_MATRIX_INV_VP_ARRAY = Shader.PropertyToID("_MainLightShadowMatrixInvVPArray");

		public static readonly int MAIN_LIGHT_SHADOWMAP_ARRAY = Shader.PropertyToID("_MainLightShadowmapArray");

		#endregion

		#region IBL Params

		public static readonly int PREINTEGRATED_DGF_LUT = Shader.PropertyToID("_PreintegratedDGFLut");
		public static readonly int PREINTEGRATED_D_LUT = Shader.PropertyToID("_PreintegratedDLut");
		public static readonly int PREINTEGRATED_GF_LUT = Shader.PropertyToID("_PreintegratedGFLut");
		public static readonly int GLOBAL_ENV_MAP_SPECULAR = Shader.PropertyToID("_GlobalEnvMapSpecular");
		public static readonly int GLOBAL_ENV_MAP_DIFFUSE = Shader.PropertyToID("_GlobalEnvMapDiffuse");
		public static readonly int GLOBAL_ENV_MAP_ROTATION = Shader.PropertyToID("_GlobalEnvMapRotation");
		public static readonly int SKYBOX_MIP_LEVEL = Shader.PropertyToID("_SkyboxMipLevel");
		public static readonly int SKYBOX_INTENSITY = Shader.PropertyToID("_SkyboxIntensity");

		#endregion

		#region Diffuse Light Probe Params

		public static readonly int DIFFUSE_PROBE_PARAMS = Shader.PropertyToID("DiffuseProbeParams");
		public static readonly int DIFFUSE_PROBE_PARAMS_0 = Shader.PropertyToID("_DiffuseProbeParams0");
		public static readonly int DIFFUSE_PROBE_PARAMS_1 = Shader.PropertyToID("_DiffuseProbeParams1");
		public static readonly int DIFFUSE_PROBE_PARAMS_2 = Shader.PropertyToID("_DiffuseProbeParams2");
		public static readonly int DIFFUSE_PROBE_GBUFFER_0_CUBEMAP = Shader.PropertyToID("_DiffuseProbeGBuffer0Cubemap");
		public static readonly int DIFFUSE_PROBE_GBUFFER_1_CUBEMAP = Shader.PropertyToID("_DiffuseProbeGBuffer1Cubemap");
		public static readonly int DIFFUSE_PROBE_GBUFFER_2_CUBEMAP = Shader.PropertyToID("_DiffuseProbeGBuffer2Cubemap");
		public static readonly int DIFFUSE_PROBE_GBUFFER_0 = Shader.PropertyToID("_DiffuseProbeGBuffer0");
		public static readonly int DIFFUSE_PROBE_GBUFFER_1 = Shader.PropertyToID("_DiffuseProbeGBuffer1");
		public static readonly int DIFFUSE_PROBE_GBUFFER_2 = Shader.PropertyToID("_DiffuseProbeGBuffer2");
		public static readonly int DIFFUSE_PROBE_VBUFFER_0 = Shader.PropertyToID("_DiffuseProbeVBuffer0");
		public static readonly int PREV_DIFFUSE_PROBE_IRRADIANCE_ARRAY = Shader.PropertyToID("_PrevDiffuseProbeIrradianceArr");
		public static readonly int DIFFUSE_PROBE_IRRADIANCE_ARRAY = Shader.PropertyToID("_DiffuseProbeIrradianceArr");
		public static readonly int DIFFUSE_PROBE_RADIANCE_ARRAY = Shader.PropertyToID("_DiffuseProbeRadianceArr");
		public static readonly int DIFFUSE_PROBE_GBUFFER_0_ARRAY = Shader.PropertyToID("_DiffuseProbeGBuffer0Arr");
		public static readonly int DIFFUSE_PROBE_GBUFFER_1_ARRAY = Shader.PropertyToID("_DiffuseProbeGBuffer1Arr");
		public static readonly int DIFFUSE_PROBE_GBUFFER_2_ARRAY = Shader.PropertyToID("_DiffuseProbeGBuffer2Arr");
		public static readonly int DIFFUSE_PROBE_VBUFFER_ARRAY = Shader.PropertyToID("_DiffuseProbeVBufferArr");
		
		// Only used in compute shaders
		public static readonly int RADIANCE_ARRAY = Shader.PropertyToID("_RadianceArr");
		public static readonly int IRRADIANCE_ARRAY = Shader.PropertyToID("_IrradianceArr");

		#endregion

		#region Render Targets

		public static readonly int MAIN_TEXTURE = Shader.PropertyToID("_MainTex");
		public static readonly int RAW_COLOR_TEXTURE = Shader.PropertyToID("_RawColorTex");
		public static readonly int COLOR_TEXTURE = Shader.PropertyToID("_ColorTex");
		public static readonly int TAA_COLOR_TEXTURE = Shader.PropertyToID("_TaaColorTex");
		public static readonly int PREV_TAA_COLOR_TEXTURE = Shader.PropertyToID("_PrevTaaColorTex");
		public static readonly int HDR_COLOR_TEXTURE = Shader.PropertyToID("_HdrColorTex");
		public static readonly int DISPLAY_TEXTURE = Shader.PropertyToID("_DisplayTex");
		public static readonly int DEPTH_TEXTURE = Shader.PropertyToID("_DepthTex");
		public static readonly int PREV_DEPTH_TEXTURE = Shader.PropertyToID("_PrevDepthTex");
		public static readonly int STENCIL_TEXTURE = Shader.PropertyToID("_StencilTex");
		public static readonly int PREV_STENCIL_TEXTURE = Shader.PropertyToID("_PrevStencilTex");
		public static readonly int VELOCITY_TEXTURE = Shader.PropertyToID("_VelocityTex");
		public static readonly int PREV_VELOCITY_TEXTURE = Shader.PropertyToID("_PrevVelocityTex");
		public static readonly int GBUFFER_0_TEXTURE = Shader.PropertyToID("_GBuffer0");
		public static readonly int GBUFFER_1_TEXTURE = Shader.PropertyToID("_GBuffer1");
		public static readonly int GBUFFER_2_TEXTURE = Shader.PropertyToID("_GBuffer2");
		public static readonly int GBUFFER_3_TEXTURE = Shader.PropertyToID("_GBuffer3");
		public static readonly int GBUFFER_4_TEXTURE = Shader.PropertyToID("_GBuffer4");
		public static readonly int SCREEN_SPACE_CUBEMAP = Shader.PropertyToID("_ScreenSpaceCubemap");
		public static readonly int SCREEN_SPACE_REFLECTION = Shader.PropertyToID("_ScreenSpaceReflection");
		public static readonly int PREV_SCREEN_SPACE_REFLECTION = Shader.PropertyToID("_PrevScreenSpaceReflection");
		public static readonly int INDIRECT_SPECULAR = Shader.PropertyToID("_IndirectSpecular");

		#endregion

		#region Color Grading & Tonemapping

		public static readonly int TONEMAPPING_MODE = Shader.PropertyToID("_TonemappingMode");
		public static readonly int COLOR_GRADE_PARAMS = Shader.PropertyToID("_ColorGradeParams");
		public static readonly int COLOR_FILTER = Shader.PropertyToID("_ColorFilter");

		#endregion

		#region Temporal Related
		
		public static readonly int ENABLE_REPROJECTION = Shader.PropertyToID("_EnableReprojection");
		public static readonly int FRAME_PARAMS = Shader.PropertyToID("_FrameParams");
		public static readonly int JITTER_PARAMS = Shader.PropertyToID("_JitterParams");
		public static readonly int TAA_PARAMS = Shader.PropertyToID("_TaaParams");

		#endregion

		#region Noises

		public static readonly int BLUE_NOISE_16 = Shader.PropertyToID("_BlueNoise16");
		public static readonly int BLUE_NOISE_64 = Shader.PropertyToID("_BlueNoise64");
		public static readonly int BLUE_NOISE_256 = Shader.PropertyToID("_BlueNoise256");
		public static readonly int BLUE_NOISE_512 = Shader.PropertyToID("_BlueNoise512");
		public static readonly int BLUE_NOISE_1024 = Shader.PropertyToID("_BlueNoise1024");

		#endregion

		#region Miscs

		#region Shader Features

		public static readonly string ACCURATE_TRANSFORM_ON = "ACCURATE_TRANSFORM_ON";

		public static GlobalKeyword MAIN_LIGHT_SHADOW_ON {
			get {
				if (main_light_shadow_on.name == null) main_light_shadow_on = GlobalKeyword.Create("MAIN_LIGHT_SHADOW_ON");
				return main_light_shadow_on;
			}
		}

		private static GlobalKeyword main_light_shadow_on;

		#endregion
		
		#region Compute Shader Kernels

		public static readonly string DIFFUSE_PROBE_GBUFFER_PREFILTER = "GBufferPrefilter";
		public static readonly string DIFFUSE_PROBE_VBUFFER_PREFILTER = "VBufferPrefilter";
		public static readonly string DIFFUSE_PROBE_VBUFFER_PADDING = "VBufferPadding";
		public static readonly string DIFFUSE_PROBE_RADIANCE_UPDATE = "RadianceUpdate";
		public static readonly string DIFFUSE_PROBE_IRRADIANCE_PREFILTER = "IrradiancePrefilter";
		public static readonly string DIFFUSE_PROBE_IRRADIANCE_PADDING = "IrradiancePadding";

		#endregion

		#endregion
	}
}