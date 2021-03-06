using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.Cameras {
	public sealed class ReflectionCameraRenderer : CameraRenderer {

		public ReflectionCameraRenderer(Camera camera) : base(camera) => cameraType = AdvancedCameraType.Reflection;

		public override void Render(ScriptableRenderContext context) {
			throw new System.NotImplementedException();
		}

		public override void Setup() {
			throw new System.NotImplementedException();
		}
	}
}