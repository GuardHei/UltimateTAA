using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class LightManager {
		// public static bool MainLightIsAvailable => mainLight != null && mainLight.isActiveAndEnabled;
		public static bool MainLightIsAvailable => mainLight != null;
		public static bool MainLightShadowAvailable => MainLightIsAvailable && mainLight.shadows != LightShadows.None;

		public static Light mainLight;

		public static DirectionalLight mainLightData {
			get;
			private set;
		}

		public static void UpdateLight(CullingResults results) {
			var visibleLights = results.visibleLights;
			var mainLightFound = false;
			for (int i = 0, l = visibleLights.Length; i < l; i++) {
				var vis = visibleLights[i];
				if (vis.lightType == LightType.Directional) {
					mainLight = vis.light;
					mainLightData = new DirectionalLight {
						direction = -vis.localToWorldMatrix.GetColumn(2),
						color = vis.finalColor.ColorToFloat4()
					};
					mainLightFound = true;
					break;
				}
			}

			if (!mainLightFound) {
				mainLight = null;
				mainLightData = new DirectionalLight {
					direction = float4.zero,
					color = float4.zero,
				};
			}
		}
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