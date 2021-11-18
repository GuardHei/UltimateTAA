using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	[CreateAssetMenu(menuName = "Advanced Render Pipeline/ARP Asset")]
	public class AdvancedRenderPipelineAsset : RenderPipelineAsset {

		public AdvancedRenderPipelineSettings settings;

		protected override RenderPipeline CreatePipeline() => new AdvancedRenderPipeline(settings);
	}

	[Serializable]
	public class AdvancedRenderPipelineSettings {
		[Header("Editor")]
		public bool enableDebugView;
		public DebugOutput debugOutput;
		public bool enablePostFXInEditor = true;
		[Header("Batch Settings")]
		public bool enableAutoInstancing = true;
		public bool enableSRPBatching = true;
		[Header("Builtin Shaders")]
		public Shader blitShader;
		[Header("Transparency"), Min(0f)]
		public float alphaTestDepthCutOff = .001f;
		[Header("Shadow"), Min(0f)]
		public float mainLightShadowDistance = 100f;
		public ShadowmapSize mainLightShadowmapSize = ShadowmapSize._2048;
		public SoftShadowMode mainLightSoftShadow = SoftShadowMode.None;
	}

	public enum DebugOutput {
		Default,
		Depth,
		GBuffer1,
		GBuffer2,
		MotionVector
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
}