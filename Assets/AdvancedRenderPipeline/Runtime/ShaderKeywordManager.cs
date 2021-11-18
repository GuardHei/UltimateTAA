using UnityEngine;

namespace AdvancedRenderPipeline.Runtime {
	public static class ShaderKeywordManager {

		#region Camera Params

		public static readonly int CAMERA_DATA = Shader.PropertyToID("CameraData");

		#endregion

		#region Light Params

		public static readonly int MAIN_LIGHT = Shader.PropertyToID("_MainLight");
		public static readonly int MAIN_LIGHT_DATA = Shader.PropertyToID("MainLightData");

		#endregion

		#region Shadow Params

		public static readonly int SHADOW_CONSTANT_BIAS = Shader.PropertyToID("_ShadowConstantBias");
		public static readonly int SHADOW_NORMAL_BIAS = Shader.PropertyToID("_ShadowNormalBias");
		public static readonly int MAIN_LIGHT_SHADOW_DIST = Shader.PropertyToID("_MainLightShadowDist");
		public static readonly int MAIN_LIGHT_SHADOW_STR = Shader.PropertyToID("_MainLightShadowStr");
		public static readonly int MAIN_LIGHT_SHADOW_TINT = Shader.PropertyToID("_MainLightShadowTint");
		public static readonly int MAIN_LIGHT_SHADOWMAP_SIZE = Shader.PropertyToID("_MainLightShadowmapSize");
		public static readonly int MAIN_LIGHT_INV_VP = Shader.PropertyToID("_MainLightInvVP");

		#endregion

		#region Render Targets

		public static readonly int MAIN_TEXTURE = Shader.PropertyToID("_MainTex");
		public static readonly int RAW_COLOR_TEXTURE = Shader.PropertyToID("_RawColorTex");
		public static readonly int TAA_COLOR_TEXTURE = Shader.PropertyToID("_TAAColorTex");
		public static readonly int HDR_COLOR_TEXTURE = Shader.PropertyToID("_HdrColorTex");
		public static readonly int DISPLAY_TEXTURE = Shader.PropertyToID("_DisplayTex");
		public static readonly int DEPTH_TEXTURE = Shader.PropertyToID("_DepthTex");
		public static readonly int STENCIL_TEXTURE = Shader.PropertyToID("_StencilTex");
		public static readonly int VELOCITY_TEXTURE = Shader.PropertyToID("_VelocityTex");
		public static readonly int GBUFFER_1_TEXTURE = Shader.PropertyToID("_GBuffer1");
		public static readonly int GBUFFER_2_TEXTURE = Shader.PropertyToID("_GBuffer2");
		public static readonly int MAIN_LIGHT_SHADOW_MAP = Shader.PropertyToID("_MainLightShadowmap");

		#endregion
	}
}