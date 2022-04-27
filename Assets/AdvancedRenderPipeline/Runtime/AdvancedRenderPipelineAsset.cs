using System;
using AdvancedRenderPipeline.Runtime.CustomAttributes;
using Unity.Mathematics;
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
		public bool enableTaaInEditor = true;
		public bool enableDebugView;
		public bool enableDebugViewInEditor;
		public DebugOutput debugOutput;
		[Header("Batch Settings")]
		public bool enableAutoInstancing = true;
		public bool enableSRPBatching = true;
		public bool enableMVAutoInstancing;
		public bool enableMVSRPBatching;
		[Header("Utilities")]
		public bool stopNaNPropagation;
		public Texture2D blueNoise16;
		public Texture2D blueNoise64;
		public Texture2D blueNoise256;
		public Texture2D blueNoise512;
		public Texture2D blueNoise1024;
		[Header("Builtin Shaders")]
		public Shader blitShader;
		public Shader integrateOpaqueLightingShader;
		public Shader cameraMotionShader;
		[Header("Transparency"), Min(0f)]
		public float alphaTestDepthCutOff = .001f;
		[Header("Shadow")]
		public ShadowSettings shadowSettings = new() {
			enabled = false,
			mainLightShadowDistance = 100f,
			mainLightShadowCascade1 = .1f,
			mainLightShadowCascade2 = .25f,
			mainLightShadowCascade3 = .5f,
			mainLightShadowmapSize = ShadowmapSize._2048,
			mainLightSoftShadow = SoftShadowMode.Pcf3x3
		};
		[Header("Image Based Lighting")]
		public Texture2D iblLut;
		public Texture2D diffuseIBLLut;
		public Texture2D specularIBLLut;
		public Cubemap globalEnvMapDiffuse;
		public Cubemap globalEnvMapSpecular;
		public Shader indirectSpecularShader;
		[Range(0f, 360.0f)]
		public float globalEnvMapRotation;
		[Range(0f, 11.0f)]
		public float skyboxMipLevel;
		[Range(0f, 3f)]
		public float skyboxIntensity = 1.0f;
		[Header("Global Illumination")]
		public DiffuseGISettings diffuseGISettings = new() {
			src = DiffuseGISource.Skybox,
			volumeCenter = Vector3.zero, dimensions = new Vector3Int(2, 2, 2), maxIntervals = Vector3.one, 
			probeGBufferSize = DiffuseGIProbeSize._16, probeVBufferSize = DiffuseGIProbeSize._16, offlineCubemapSize = DiffuseGIProbeSize._512,
			probeViewDistance = 50f, probeDepthSharpness = 80f, probeIrradianceGamma = 1f,
			probeGBufferPath0 = "probe_gbuffer_0", probeGBufferPath1 = "probe_gbuffer_1", probeGBufferPath2 = "probe_gbuffer_2", probeVBufferPath0 = "probe_vbuffer_0"
		};
		[Header("Anti Aliasing")]
		public TemporalAntiAliasingSettings taaSettings = new() {
			enabled = true, jitterNum = JitterNum._8, jitterSpread = .75f, 
			minHistoryWeight = .6f, maxHistoryWeight = .95f, minClipScale = .5f, maxClipScale = 1.25f, 
			minVelocityRejection = 1f, velocityRejectionScale = 0f, minDepthRejection = 1f, depthRejectionScale = 0f, 
			minSharpness = .25f, maxSharpness = .25f
		};
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
		GBuffer3,
		Smoothness,
		Velocity,
		ScreenSpaceCubemap,
		ScreenSpaceReflection,
		ScreenSpaceReflectionHistory,
		IndirectSpecular,
		RawColor,
		Color,
		TaaColor,
		TaaColorHistory,
		HDRColor,
		NaN
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
		Pcf3x3 = 0,
		Pcf5x5 = 1,
		Pcss = 2
	}

	public enum TonemappingMode {
		None = 0,
		ACES = 1,
		Neutral = 2,
		Reinhard = 3
	}

	public enum JitterNum {
		_2 = 2,
		_4 = 4,
		_8 = 8,
		_16 = 16
	}

	[Serializable]
	public struct ShadowSettings {
		public bool enabled;
		public float mainLightShadowDistance;
		[Range(0f, 1f)]
		public float mainLightShadowCascade1;
		[Range(0f, 1f)]
		public float mainLightShadowCascade2;
		[Range(0f, 1f)]
		public float mainLightShadowCascade3;
		public ShadowmapSize mainLightShadowmapSize;
		public SoftShadowMode mainLightSoftShadow;

		public Vector3 MainLightShadowCascades => new(mainLightShadowCascade1, mainLightShadowCascade2, mainLightShadowCascade3);
	}

	[Serializable]
	public struct TemporalAntiAliasingSettings {
		public bool enabled;
		public JitterNum jitterNum;
		[Range(0f, 1f)]
		public float jitterSpread;
		[Range(0f, 1f)]
		public float minHistoryWeight;
		[Range(0f, 1f)]
		public float maxHistoryWeight;
		[Range(.05f, 6f)]
		public float minClipScale;
		[Range(.05f, 6f)]
		public float maxClipScale;
		[Tooltip("Used for anti-flickering")]
		[Range(.05f, 12f)]
		public float staticClipScale;
		[Range(0f, 1f)]
		public float minVelocityRejection;
		[Range(0f, 10f)]
		public float velocityRejectionScale;
		[Tooltip("Distance in eye space")]
		[Range(0f, 50f)]
		public float minDepthRejection;
		[Range(0f, 2f)]
		public float depthRejectionScale;
		[Range(0f, 2f)]
		public float minSharpness;
		[Range(0f, 2f)]
		public float maxSharpness;
		[Range(0f, 10f)]
		public float motionSharpeningFactor;

		public Vector4 TaaParams0 => new(minHistoryWeight, maxHistoryWeight, minClipScale, maxClipScale);
		public Vector4 TaaParams1 => new(minVelocityRejection, velocityRejectionScale, minDepthRejection, depthRejectionScale);
		public Vector4 TaaParams2 => new(minSharpness, maxSharpness, motionSharpeningFactor, staticClipScale);
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

	[Serializable]
	public struct DiffuseGISettings {
		[Tooltip("Is the dynamic diffuse gi enabled by the user?")]
		public bool enabled;
		[Tooltip("Click to ask the pipeline to re-load the probe buffers from disks")]
		public bool markProbesDirty;
		[DisplayOnly]
		[Tooltip("Is the dynamic diffuse gi actually enabled?")]
		public bool enabledFlag;
		public DiffuseGISource src;
		public Vector3 volumeCenter;
		public Vector3Int dimensions;
		public Vector3 maxIntervals;
		public DiffuseGIProbeSize probeGBufferSize;
		public DiffuseGIProbeSize probeVBufferSize;
		public DiffuseGIProbeSize offlineCubemapSize;
		[Range(.1f, 250f)]
		public float probeViewDistance;
		[Range(0f, 200f)]
		public float probeDepthSharpness;
		[Range(0f, 5f)]
		public float probeIrradianceGamma;
		public string probeGBufferPath0;
		public string probeGBufferPath1;
		public string probeGBufferPath2;
		public string probeVBufferPath0;
		public ComputeShader offlineComputeShader;
		public ComputeShader runtimeComputeShader;
		public Texture2DArray probeGBufferArr0;
		public Texture2DArray probeGBufferArr1;
		public Texture2DArray probeGBufferArr2;
		public Texture2DArray probeVBufferArr0;

		public bool Enabled => enabled && enabledFlag;

		public int Count => dimensions.x * dimensions.y * dimensions.z;
		
		public Vector3 Sizes => new((dimensions.x - 1f) * maxIntervals.x, (dimensions.y - 1f) * maxIntervals.y, (dimensions.z - 1f) * maxIntervals.z);

		public Vector3 Min => volumeCenter - Sizes * .5f;
		
		public Vector3 Max => volumeCenter + Sizes * .5f;

		public float DiagonalLength => Vector3.Distance(Max, Min);

		public float GridDiagonalLength => Mathf.Sqrt(maxIntervals.x * maxIntervals.x + maxIntervals.y * maxIntervals.y + maxIntervals.z * maxIntervals.z);

		public DiffuseProbeParams GPUParams {
			get {
				var gpuParams = new DiffuseProbeParams {
					_DiffuseProbeParams0 = new float4(volumeCenter.x, volumeCenter.y, volumeCenter.z, probeViewDistance),
					_DiffuseProbeParams1 = new float4(dimensions.x, dimensions.y, dimensions.z, probeDepthSharpness),
					_DiffuseProbeParams2 = new float4(maxIntervals.x, maxIntervals.y, maxIntervals.z, GridDiagonalLength),
					_DiffuseProbeParams3 = new int4((int) probeGBufferSize, (int) probeVBufferSize, (int) offlineCubemapSize, Enabled ? 1 : 0),
					_DiffuseProbeParams4 = new float4(Min, probeIrradianceGamma),
					_DiffuseProbeParams5 = new float4(Max, (int) src)
				};
				return gpuParams;
			}
		}
		
		public int GetProbeIndex1d(Vector3Int probe) => (probe.z * dimensions.x * dimensions.y) + (probe.y * dimensions.x) + probe.x;
		
		public Vector3Int GetProbeIndex3d(int index) {
			var z = index / (dimensions.x * dimensions.y);
			index -= (z * dimensions.x * dimensions.y);
			var y = index / dimensions.x;
			var x = index % dimensions.x;
			return new Vector3Int(x, y, z);
		}
	}

	public enum DiffuseGIProbeSize {
		_6 = 6,
		_8 = 8,
		_16 = 16,
		_24 = 24,
		_32 = 32,
		_64 = 64,
		_128 = 128,
		_256 = 256,
		_512 = 512,
		_1024 = 1024
	}

	public enum DiffuseGISource {
		None = 0,
		Skybox = 1,
		DDGI = 2
	}
}