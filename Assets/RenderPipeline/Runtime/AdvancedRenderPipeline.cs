using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class AdvancedRenderPipeline : RenderPipeline {

	public static AdvancedRenderPipelineSettings settings;
	
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

	public AdvancedRenderPipeline(AdvancedRenderPipelineSettings settings) {
		AdvancedRenderPipeline.settings = settings;
	}

	protected override void Render(ScriptableRenderContext context, Camera[] cameras) {

		var screenWidth = Screen.width;
		var screenHeight = Screen.height;
		
		foreach (var camera in cameras) {
			var cameraRenderer = GetCameraRenderer(camera);
			cameraRenderer.SetResolutionAndRatio(screenWidth, screenHeight, 1f, 1f);
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
