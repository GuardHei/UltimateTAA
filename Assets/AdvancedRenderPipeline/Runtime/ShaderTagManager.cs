using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class ShaderTagManager {

		public static readonly ShaderTagId[] LEGACY_SHADER_TAGS = {
			new ShaderTagId("Always"),
			new ShaderTagId("ForwardBase"),
			new ShaderTagId("PrepassBase"),
			new ShaderTagId("Vertex"),
			new ShaderTagId("VertexLMRGBM"),
			new ShaderTagId("VertexLM")
		};

		public static readonly ShaderTagId NONE = ShaderTagId.none;
		public static readonly ShaderTagId SRP_DEFAULT_UNLIT = new ShaderTagId("SRPDefaultUnlit");
		public static readonly ShaderTagId DEPTH = new ShaderTagId("Depth");
		public static readonly ShaderTagId STENCIL = new ShaderTagId("Stencil");
		public static readonly ShaderTagId DEPTH_STENCIL = new ShaderTagId("DepthStencil");
		public static readonly ShaderTagId SHADOW_CASTER = new ShaderTagId("ShadowCaster");
		public static readonly ShaderTagId FORWARD = new ShaderTagId("Forward");
	}
}
