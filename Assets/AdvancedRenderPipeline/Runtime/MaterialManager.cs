using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRenderPipeline.Runtime {
	public class MaterialManager {

		#region Materials
		
		public static Material BlitMat {
			get {
				if (blitMat == null) {
					if (!AdvancedRenderPipeline.settings.blitShader) AdvancedRenderPipeline.settings.blitShader = Shader.Find("Hidden/ARPBlit");
					blitMat = new Material(AdvancedRenderPipeline.settings.blitShader) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				return blitMat;
			}
		}
		
		private static Material blitMat;

		public static Material TonemappingMat {
			get {
				if (tonemappingMat == null) {
					if (!AdvancedRenderPipeline.settings.tonemappingSettings.tonemappingShader) AdvancedRenderPipeline.settings.tonemappingSettings.tonemappingShader = Shader.Find("Hidden/ARPTonemapping");
					tonemappingMat = new Material(AdvancedRenderPipeline.settings.tonemappingSettings.tonemappingShader) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				return tonemappingMat;
			}
		}

		private static Material tonemappingMat;

		public static Material ErrorMat {
			get {
				if (errorMat == null) errorMat = new Material(Shader.Find("Hidden/InternalErrorShader")) { hideFlags = HideFlags.HideAndDontSave };
				return errorMat;
			}
		}

		private static Material errorMat;
		
		public static Material IndirectSpecularMat {
			get {
				if (indirectSpecularMat == null) {
					if (!AdvancedRenderPipeline.settings.indirectSpecularShader) AdvancedRenderPipeline.settings.indirectSpecularShader = Shader.Find("Hidden/ARPIndirectSpecular");
					indirectSpecularMat = new Material(AdvancedRenderPipeline.settings.indirectSpecularShader) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return indirectSpecularMat;
			}
		}

		private static Material indirectSpecularMat;
		
		public static Material IntegrateOpaqueLightingMat {
			get {
				if (integrateOpaqueLightingMat == null) {
					if (!AdvancedRenderPipeline.settings.integrateOpaqueLightingShader) AdvancedRenderPipeline.settings.integrateOpaqueLightingShader = Shader.Find("Hidden/ARPIntegrateOpaqueLighting");
					integrateOpaqueLightingMat = new Material(AdvancedRenderPipeline.settings.integrateOpaqueLightingShader) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return integrateOpaqueLightingMat;
			}
		}

		private static Material integrateOpaqueLightingMat;
		
		#endregion

		#region Passes

		public static readonly int CUBEMAP_REFLECTION_PASS = IndirectSpecularMat.FindPass("CubemapReflection");
		public static readonly int SCREEN_SPACE_REFLECTION_PASS = IndirectSpecularMat.FindPass("ScreenSpaceReflection");
		public static readonly int INTEGRATE_INDIRECT_SPECULAR_PASS = IndirectSpecularMat.FindPass("IntegrateIndirectSpecular");
		public static readonly int INTEGRATE_OPAQUE_LIGHTING_PASS = IntegrateOpaqueLightingMat.FindPass("IntegrateOpaqueLighting");

		#endregion
	}
}