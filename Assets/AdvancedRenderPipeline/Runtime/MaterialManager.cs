using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRenderPipeline.Runtime {
	public class MaterialManager {
		
		public static Material BlitMaterial {
			get {
				if (blitMaterial == null) {
					if (!AdvancedRenderPipeline.settings.blitShader) AdvancedRenderPipeline.settings.blitShader = Shader.Find("Hidden/ARPBlit");
					blitMaterial = new Material(AdvancedRenderPipeline.settings.blitShader) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				return blitMaterial;
			}
		}
		
		private static Material blitMaterial;

		public static Material TonemappingMaterial {
			get {
				if (tonemappingMaterial == null) {
					if (!AdvancedRenderPipeline.settings.tonemappingSettings.tonemappingShader) AdvancedRenderPipeline.settings.tonemappingSettings.tonemappingShader = Shader.Find("Hidden/ARPTonemapping");
					tonemappingMaterial = new Material(AdvancedRenderPipeline.settings.tonemappingSettings.tonemappingShader) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				return tonemappingMaterial;
			}
		}

		private static Material tonemappingMaterial;

		public static Material ErrorMat {
			get {
				if (_errorMat == null) _errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));
				return _errorMat;
			}
		}

		private static Material _errorMat;
	}
}