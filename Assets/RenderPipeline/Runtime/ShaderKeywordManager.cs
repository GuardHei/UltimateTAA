using UnityEngine;

public static class ShaderKeywordManager {

	#region Camera Params

	public static readonly int CAMERA_POSITION = Shader.PropertyToID("_CameraPosition");
	public static readonly int CAMERA_FORWARD = Shader.PropertyToID("_CameraForward");

	#endregion

	#region Light Params

	public static readonly int MAIN_LIGHT_DIRECTION = Shader.PropertyToID("_MainLightDirection");
	public static readonly int MAIN_LIGHT_COLOR = Shader.PropertyToID("_MainLightColor");

	#endregion

	#region Shadow Params

	public static readonly int SHADOW_CONSTANT_BIAS = Shader.PropertyToID("_ShadowConstantBias");
	public static readonly int SHADOW_NORMAL_BIAS = Shader.PropertyToID("_ShadowNormalBias");
	public static readonly int MAIN_LIGHT_SHADOW_DISTANCE = Shader.PropertyToID("_MainLightShadowDistance");
	public static readonly int MAIN_LIGHT_SHADOW_STRENGTH = Shader.PropertyToID("_MainLightShadowStrength");
	public static readonly int MAIN_LIGHT_SHADOW_TINT = Shader.PropertyToID("_MainLightShadowTint");
	public static readonly int MAIN_LIGHT_SHADOWMAP_SIZE = Shader.PropertyToID("_MainLightShadowmapSize");
	public static readonly int MAIN_LIGHT_INVERSE_VP = Shader.PropertyToID("_MainLightInverseVP");

	#endregion

	#region Render Targets

	public static readonly int RAW_COLOR_TEXTURE = Shader.PropertyToID("_RawColorTexture");
	public static readonly int COLOR_TEXTURE = Shader.PropertyToID("_ColorTexture");
	public static readonly int DEPTH_TEXTURE = Shader.PropertyToID("_DepthTexture");
	public static readonly int NORMAL_TEXTURE = Shader.PropertyToID("_NormalTexture");
	public static readonly int MAIN_LIGHT_SHADOW_MAP = Shader.PropertyToID("_MainLightShadowmap");

	#endregion
}
