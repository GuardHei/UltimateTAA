using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class ARPUtils {

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