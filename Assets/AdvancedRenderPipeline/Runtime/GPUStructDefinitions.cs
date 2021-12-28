using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	
	public static class GPUStructDefinitions {
		
	}
	
	[Serializable]
	public struct PackedRTHandleProperties {
		public int4 viewportSize;
		public int4 rtSize;
		public float4 rtHandleScale;
	}

	[Serializable]
	public struct CameraData {
		public float3 cameraPosWS;
		public float3 cameraFwdWS;
		public float4 screenSize;
		public PackedRTHandleProperties _rtHandleProps;
	}

	[Serializable]
	public struct DirectionalLight {
		public float4 direction;
		public float4 color; // rgb - final light color, a - unused
	}
}