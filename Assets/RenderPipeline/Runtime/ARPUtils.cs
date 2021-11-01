using UnityEngine;
using UnityEngine.Rendering;

public static class ARPUtils {

	public static RenderTargetIdentifier[] RTHandlesToRTIs(RTHandle[] rtHandles) {
		var len = rtHandles.Length;
		var rts = new RenderTargetIdentifier[len];
		for (int i = 0; i < len; i++) rts[i] = rtHandles[i];
		return rts;
	}
}