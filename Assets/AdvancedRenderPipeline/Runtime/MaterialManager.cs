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

		public static Material ErrorMat {
			get {
				if (_errorMat == null) _errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));
				return _errorMat;
			}
		}

		private static Material _errorMat;
	}
}