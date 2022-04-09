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
					indirectSpecularMat.DisableKeyword(ShaderKeywordManager.ACCURATE_TRANSFORM_ON);
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

		public static Material CameraMotionMat {
			get {
				if (cameraMotionMat == null) {
					if (!AdvancedRenderPipeline.settings.cameraMotionShader) AdvancedRenderPipeline.settings.cameraMotionShader = Shader.Find("Hidden/ARPCameraMotion");
					cameraMotionMat = new Material(AdvancedRenderPipeline.settings.cameraMotionShader) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return cameraMotionMat;
			}
		}

		private static Material cameraMotionMat;

		public static Material TaaMat {
			get {
				if (taaMat == null) {
					taaMat = new Material(Shader.Find("Hidden/ARPTemporalAntiAliasing")) {
						hideFlags = HideFlags.HideAndDontSave
					};
				}
				return taaMat;
			}
		}
		
		private static Material taaMat;
		
		#endregion

		#region Passes
		
		public static readonly int SCREEN_SPACE_REFLECTION_PASS = IndirectSpecularMat.FindPass("ScreenSpaceReflection");
		public static readonly int CUBEMAP_REFLECTION_PASS = IndirectSpecularMat.FindPass("CubemapReflection");
		public static readonly int INTEGRATE_OPAQUE_LIGHTING_PASS = IntegrateOpaqueLightingMat.FindPass("IntegrateOpaqueLighting");
		public static readonly int CAMERA_MOTION_VECTORS_PASS = CameraMotionMat.FindPass("CameraMotionVectors");
		public static readonly int STOP_NAN_PROPAGATION_PASS = TonemappingMat.FindPass("StopNaNPropagation");
		public static readonly int FAST_TONEMAPPING_PASS = TonemappingMat.FindPass("FastTonemapping");
		public static readonly int FAST_INVERT_TONEMAPPING_PASS = TonemappingMat.FindPass("FastInvertTonemapping");
		public static readonly int TONEMAPPING_PASS = TonemappingMat.FindPass("Tonemapping");
		public static readonly int TEMPORAL_ANTI_ALIASING_PASS = TaaMat.FindPass("TemporalAntiAliasing");

		#endregion
	}
}