using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	[CreateAssetMenu(fileName = "ARP Asset", menuName = "Advanced Render Pipeline/ARP Asset")]
	public class AdvancedRenderPipelineAsset : RenderPipelineAsset {

		public AdvancedRenderPipelineSettings settings;

		protected override RenderPipeline CreatePipeline() => new AdvancedRenderPipeline(settings);

		public override Shader defaultShader {
			get {
				if (standardShader == null) standardShader = Shader.Find("Advanced Render Pipeline/ARPStandard");
				return standardShader;
			}
		}

		public override Material defaultMaterial {
			get {
				var mat = new Material(defaultShader);
				mat.SetShaderPassEnabled(ShaderTagManager.MOTION_VECTORS_PASS, false);
				return mat;
			}
		}

		private static Shader standardShader;
	}

	[Serializable]
	public class AdvancedRenderPipelineSettings {
		[Header("Editor")]
		public bool enableDebugView;
		public bool enableDebugViewInEditor;
		public DebugOutput debugOutput;
		[Header("Batch Settings")]
		public bool enableAutoInstancing = true;
		public bool enableSRPBatching = true;
		public bool enableMVAutoInstancing;
		public bool enableMVSRPBatching;
		[Header("Builtin Shaders")]
		public Shader blitShader;
		public Shader integrateOpaqueLightingShader;
		public Shader cameraMotionShader;
		[Header("Transparency"), Min(0f)]
		public float alphaTestDepthCutOff = .001f;
		[Header("Shadow"), Min(0f)]
		public float mainLightShadowDistance = 100f;
		public ShadowmapSize mainLightShadowmapSize = ShadowmapSize._2048;
		public SoftShadowMode mainLightSoftShadow = SoftShadowMode.None;
		[Header("Image Based Lighting")]
		public Texture2D iblLut;
		public Texture2D diffuseIBLLut;
		public Texture2D specularIBLLut;
		public Cubemap globalEnvMapDiffuse;
		public Cubemap globalEnvMapSpecular;
		public Shader indirectSpecularShader;
		[Range(.0f, 360.0f)]
		public float globalEnvMapRotation;
		[Range(.0f, 11.0f)]
		public float skyboxMipLevel;
		[Header("Anti Aliasing")]
		public TemporalAntiAliasingSettings taaSettings = new() { enabled = true, jitterSpread = .75f, minHistoryWeight = .6f, maxHistoryWeight = .95f };
		[Header("Color Grading & Tonemapping")]
		public ColorGradingSettings colorSettings = new() { colorFilter = Color.white };
		public TonemappingSettings tonemappingSettings;
	}

	public enum DebugOutput {
		Default,
		Depth,
		Stencil,
		GBuffer1,
		GBuffer2,
		Velocity,
		ScreenSpaceCubemap,
		ScreenSpaceReflection,
		ScreenSpaceReflectionHistory,
		IndirectSpecular,
		RawColor,
		Color,
		TaaColor,
		TaaColorHistory,
		HDRColor
	}

	public enum ShadowmapSize {
		_256 = 256,
		_512 = 512,
		_1024 = 1024,
		_2048 = 2048,
		_4096 = 4096,
		_8192 = 8192
	}

	public enum SoftShadowMode {
		None = 0,
		Pcf = 1,
		Pcss = 2
	}

	public enum TonemappingMode {
		None = 0,
		ACES = 1,
		Neutral = 2,
		Reinhard = 3
	}

	[Serializable]
	public struct TemporalAntiAliasingSettings {
		public bool enabled;
		[Range(0f, 1f)]
		public float jitterSpread;
		[Range(0f, 1f)]
		public float minHistoryWeight;
		[Range(0f, 1f)]
		public float maxHistoryWeight;
		public float minSharpness;
		public float maxSharpness;
	}

	[Serializable]
	public struct ColorGradingSettings {
		[Range(-10f, 10f)]
		public float postExposure;
		[Range(-100f, 100f)]
		public float contrast;
		[ColorUsage(false, true)]
		public Color colorFilter;
		[Range(-180f, 180f)]
		public float hueShift;
		[Range(-100f, 100f)]
		public float saturation;
	}

	[Serializable]
	public struct TonemappingSettings {
		public TonemappingMode tonemappingMode;
		public Shader tonemappingShader;
	}
}