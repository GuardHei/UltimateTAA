using System;
using Unity.Mathematics;
using UnityEngine;

namespace AdvancedRenderPipeline.Runtime {
	
	public static class GPUStructManager {
		
	}

	[Serializable]
	public struct CameraData {
		public float3 cameraPosWS;
		public float3 cameraFwdWS;
		public float4 screenSize;
		public PackedRTHandleProperties _rtHandleProps;
	}
	
	[Serializable]
	public struct PackedRTHandleProperties {
		public int4 viewportSize;
		public int4 rtSize;
		public float4 rtHandleScale;
	}
	
	[Serializable]
	public class MainLightParams {
		public bool enabled = true;
		public bool shadowOn = true;
		public int shadowDistance = 100;
		public int shadowResolution = 2048;
		public int shadowCascades = 4;
		public Vector3 shadowCascadeSplits = new Vector3(.067f, .2f, .467f);
	}

	[Serializable]
	public struct DirectionalLight {
		public float4 direction;
		public float4 color; // rgb - final light color, a - unused
	}
}