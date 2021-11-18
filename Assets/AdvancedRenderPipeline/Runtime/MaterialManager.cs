using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRenderPipeline.Runtime {
	public class MaterialManager {
		
		public static Material BlitMaterial {
			get {
				if (blitMaterial == null && AdvancedRenderPipeline.settings.blitShader != null) {
					blitMaterial = new Material(AdvancedRenderPipeline.settings.blitShader);
					blitMaterial.hideFlags = HideFlags.HideAndDontSave;
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