using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class LightManager {
		// public static bool MainLightIsAvailable => MainLight != null && MainLight.isActiveAndEnabled;
		public static bool MainLightIsAvailable => MainLight != null;
		public static bool GIMainLightIsAvailable => GIMainLight != null;
		public static bool MainLightShadowAvailable => MainLightIsAvailable && MainLight.shadows != LightShadows.None;

		public static Light MainLight {
			get => mainLight;
			set {
				mainLight = value;
				if (value == null) {
					MainLightData = new DirectionalLight {
						direction = float4.zero,
						color = float4.zero,
					};
				} else {
					MainLightData = new DirectionalLight {
						direction = -mainLight.transform.localToWorldMatrix.GetColumn(2),
						color = (mainLight.color.linear * mainLight.intensity).ColorToFloat4()
					};
				}
			}
		}

		public static Light GIMainLight {
			get => giMainLight;
			set {
				giMainLight = value;
				if (value == null) {
					GIMainLightData = new DirectionalLight {
						direction = float4.zero,
						color = float4.zero,
					};
				} else {
					GIMainLightData = new DirectionalLight {
						direction = -giMainLight.transform.localToWorldMatrix.GetColumn(2),
						color = (giMainLight.color.linear * giMainLight.intensity).ColorToFloat4()
					};
				}
			}
		}

		public static DirectionalLight MainLightData {
			get;
			private set;
		}
		
		public static DirectionalLight GIMainLightData {
			get;
			private set;
		}

		private static Light mainLight;
		private static Light giMainLight;

		public static void UpdateLight(CullingResults results) {
			var visibleLights = results.visibleLights;
			var mainLightFound = false;
			for (int i = 0, l = visibleLights.Length; i < l; i++) {
				var vis = visibleLights[i];
				if (vis.lightType == LightType.Directional) {
					MainLight = vis.light;
					MainLightData = new DirectionalLight {
						direction = -vis.localToWorldMatrix.GetColumn(2),
						color = vis.finalColor.ColorToFloat4()
					};
					mainLightFound = true;
					break;
				}
			}

			if (!mainLightFound) MainLight = null;
		}
	}
}