using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class ARPUtils {

		public static Vector3 GetViewForward(this Matrix4x4 mat) => -mat.GetRow(2);
		
		public static Vector3 GetViewUp(this Matrix4x4 mat) => mat.GetRow(1);
		
		public static Vector3 GetViewRight(this Matrix4x4 mat) => mat.GetRow(0);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float4 ColorToFloat4(this Color color) => new float4(color.r, color.g, color.b, color.a);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int4 Vector2IntToInt4(Vector2Int a, Vector2Int b) => new int4(a.x, a.y, b.x, b.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PackedRTHandleProperties Pack(this RTHandleProperties properties) {
			return new PackedRTHandleProperties {
				viewportSize = new int4(Vector2IntToInt4(properties.currentViewportSize, properties.previousViewportSize)),
				rtSize = new int4(Vector2IntToInt4(properties.currentRenderTargetSize, properties.previousRenderTargetSize)),
				rtHandleScale = properties.rtHandleScale
			};
		}
	}
}