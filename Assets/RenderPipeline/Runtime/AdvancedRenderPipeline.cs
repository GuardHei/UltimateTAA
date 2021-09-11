using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class AdvancedRenderPipeline : RenderPipeline {

	internal static Dictionary<Camera, CameraRenderer> cameraRenderers = new Dictionary<Camera, CameraRenderer>(2);

	#region Static Methods

	public static bool RemoveCamera(Camera camera) => cameraRenderers.Remove(camera);

	public static bool RegisterCamera(Camera camera) => RegisterCamera(camera, CameraRenderer.DefaultToAdvancedCameraType(camera.cameraType));

	public static bool RegisterCamera(Camera camera, AdvancedCameraType type) {
		if (!cameraRenderers.ContainsKey(camera)) {
			cameraRenderers.Add(camera, CameraRenderer.CreateCameraRenderer(camera, type));
			return true;
		}

		return false;
	}

	#endregion

	protected override void Render(ScriptableRenderContext context, Camera[] cameras) {

		foreach (var camera in cameras) {
			var cameraRenderer = GetCameraRenderer(camera);
			cameraRenderer.Render(context);
		}
	}

	internal CameraRenderer GetCameraRenderer(Camera camera) {
		if (!cameraRenderers.TryGetValue(camera, out var renderer)) {
			renderer = CameraRenderer.CreateCameraRenderer(camera, CameraRenderer.DefaultToAdvancedCameraType(camera.cameraType));
			cameraRenderers.Add(camera, renderer);
		}

		return renderer;
	}
}
