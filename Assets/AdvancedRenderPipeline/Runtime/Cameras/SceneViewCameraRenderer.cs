using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.Cameras {
#if UNITY_EDITOR
	public class SceneViewCameraRenderer : GameCameraRenderer {
		
		public SceneViewCameraRenderer(Camera camera) : base(camera) {
			// Debug.Log("SceneView camera render is initing...");
			cameraType = AdvancedCameraType.SceneView;
			_enableTaa = settings.enableTaaInEditor;
			_rendererDesc = "Render Scene View (" + camera.name + ")";
			beforeCull += EmitUIMesh;
			beforePostProcess += DrawPreImageGizmosPass;
			afterLastPass += DrawUnsupportedShaders;
			afterLastPass += DrawPostImageGizmosPass;
		}

		public void EmitUIMesh() => ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);

		public void DrawPreImageGizmosPass() {
			if (Handles.ShouldRenderGizmos()) {
				_context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			}
		}

		public void DrawPostImageGizmosPass() {
			if (Handles.ShouldRenderGizmos()) {
				_context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
			}
		}

		public void DrawUnsupportedShaders() {
			var drawSettings = new DrawingSettings {
				sortingSettings = new SortingSettings(camera),
				overrideMaterial = MaterialManager.ErrorMat
			};

			for (var i = 0; i < ShaderTagManager.LEGACY_SHADER_TAGS.Length; i++)
				drawSettings.SetShaderPassName(i, ShaderTagManager.LEGACY_SHADER_TAGS[i]);

			var filterSettings = new FilteringSettings(RenderQueueManager.ALL_QUEUE);

			_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
		}
	}
#else
	public class SceneViewCameraRenderer : GameCameraRenderer {
		public SceneViewCameraRenderer(Camera camera) : base(camera) {
			// Debug.Log("SceneView camera render is initing...");
			cameraType = AdvancedCameraType.SceneView;
			_enableTaa = settings.enableTaaInEditor;
			_rendererDesc = "Render Scene View (" + camera.name + ")";
		}
	}
#endif
}