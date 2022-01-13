using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.Cameras {
#if UNITY_EDITOR
    public class PreviewCameraRenderer : GameCameraRenderer {
		
        public PreviewCameraRenderer(Camera camera) : base(camera) {
            // Debug.Log("Preview camera render is initing...");
            cameraType = AdvancedCameraType.Preview;
            _enableTaa = false;
            _rendererDesc = "Render Preview (" + camera.name + ")";
        }
    }
#else
	public class PreviewCameraRenderer : GameCameraRenderer {
        public PreviewCameraRenderer(Camera camera) : base(camera) {
            // Debug.Log("Preview camera render is initing...");
            cameraType = AdvancedCameraType.Preview;
            _enableTaa = false;
            _rendererDesc = "Render Preview (" + camera.name + ")";
        }
	}
#endif
}