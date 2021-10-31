using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ReflectionCameraRenderer : CameraRenderer {
	public ReflectionCameraRenderer(Camera camera) : base(camera) => cameraType = AdvancedCameraType.Reflection;
	public override void Render(ScriptableRenderContext context) {
		throw new System.NotImplementedException();
	}

	public override void Setup() {
		throw new System.NotImplementedException();
	}
}
