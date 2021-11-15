using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class ARPUtils {

		public static float4 ColorToFloat4(this Color color) => new float4(color.r, color.g, color.b, color.a);
		public static Vector4 ColorToVector4(this Color color) => new Vector4(color.r, color.g, color.b, color.a);

		public static RenderTargetIdentifier[] RTHandlesToRTIs(RTHandle[] rtHandles) {
			var len = rtHandles.Length;
			var rts = new RenderTargetIdentifier[len];
			for (int i = 0; i < len; i++) rts[i] = rtHandles[i];
			return rts;
		}

		public static void RTHandlesToRTIsNonAlloc(RTHandle[] rtHandles, ref RenderTargetIdentifier[] rts) {
			var len = Math.Min(rtHandles.Length, rts.Length);
			for (int i = 0; i < len; i++) rts[i] = rtHandles[i];
		}
	}
}