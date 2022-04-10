using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class ShaderTagManager {

		#region ShaderTagIds
		
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
		public static readonly ShaderTagId OCCLUDER_DEPTH = new ShaderTagId("OccluderDepth");
		public static readonly ShaderTagId MOTION_VECTORS = new ShaderTagId("MotionVectors");
		public static readonly ShaderTagId SHADOW_CASTER = new ShaderTagId("ShadowCaster");
		public static readonly ShaderTagId FORWARD = new ShaderTagId("Forward");
		public static readonly ShaderTagId OPAQUE_FORWARD = new ShaderTagId("OpaqueForward");
		public static readonly ShaderTagId DIFFUSE_PROBE_GBUFFER = new ShaderTagId("DiffuseProbeGBuffer");
		
		#endregion

		#region Shader Pass Names

		public const string MOTION_VECTORS_PASS = "MotionVectors";

		#endregion
	}
}
