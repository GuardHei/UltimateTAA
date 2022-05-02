using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public static class LightManager {
		// public static bool MainLightIsAvailable => MainLight != null && MainLight.isActiveAndEnabled;
		public static bool MainLightIsAvailable => MainLight.light != null;
		public static bool GIMainLightIsAvailable => GIMainLight != null;
		public static bool MainLightShadowAvailable => MainLightIsAvailable && MainLight.light.shadows != LightShadows.None;

		public static int MainLightIndex {
			get;
			private set;
		}

		public static VisibleLight MainLight {
			get => mainLight;
			set {
				mainLight = value;
				MainLightData = new DirectionalLight {
					direction = -mainLight.localToWorldMatrix.GetColumn(2),
					color = mainLight.finalColor.ColorToFloat4()
				};
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

		private static VisibleLight mainLight;
		private static Light giMainLight;

		public static void UpdateLight(CullingResults results) {
			var visibleLights = results.visibleLights;
			var mainLightFound = false;
			for (int i = 0, l = visibleLights.Length; i < l; i++) {
				var vis = visibleLights[i];
				if (vis.lightType == LightType.Directional) {
					MainLightIndex = i;
					MainLight = vis;
					mainLightFound = true;
					break;
				}
			}

			if (!mainLightFound) {
				MainLightIndex = -1;
				MainLight = new VisibleLight {
					finalColor = Color.clear
				};
			}
		}
	}
}