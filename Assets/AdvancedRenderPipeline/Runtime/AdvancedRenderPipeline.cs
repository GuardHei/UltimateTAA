using System;
using System.Collections.Generic;
using AdvancedRenderPipeline.Runtime.Cameras;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
	public sealed class AdvancedRenderPipeline : RenderPipeline {

		public static AdvancedRenderPipeline instance { get; private set; }

		public static bool ReversedZ { get; private set; }

		public static AdvancedRenderPipelineSettings settings;

		private static readonly Dictionary<Camera, CameraRenderer> cameraRenderers = new Dictionary<Camera, CameraRenderer>(2);

		private static readonly List<KeyValuePair<Camera, CameraRenderer>> tempCameras = new List<KeyValuePair<Camera, CameraRenderer>>(10);

		private static readonly Dictionary<CommandBuffer, Action> independentCMDRequests = new Dictionary<CommandBuffer, Action>();

		#region Static Methods

		public static void RequestCameraCheck() {
			foreach (var pair in cameraRenderers) {
				if (!pair.Key || pair.Value == null) tempCameras.Add(pair);
			}

			foreach (var pair in tempCameras) {
				cameraRenderers.Remove(pair.Key);
				pair.Value.Dispose();
			}
			
			tempCameras.Clear();
		}

		public static bool RemoveCamera(Camera camera) {
			if (!camera) return false;
			if (cameraRenderers.TryGetValue(camera, out var renderer)) {
				renderer.Dispose();
				return cameraRenderers.Remove(camera);
			}

			return false;
		}

		public static bool RegisterCamera(Camera camera) =>
			RegisterCamera(camera, CameraRenderer.DefaultToAdvancedCameraType(camera.cameraType));

		public static bool RegisterCamera(Camera camera, AdvancedCameraType type) {
			if (cameraRenderers.ContainsKey(camera)) return false;
			cameraRenderers.Add(camera, CameraRenderer.CreateCameraRenderer(camera, type));
			return true;

		}

		public static bool AddIndependentCommandBufferRequest(CommandBuffer request, Action onSubmit) {
			if (instance == null) return false;
			independentCMDRequests.Add(request, onSubmit);
			return true;
		}

		private static void ExecuteIndependentCommandBufferRequest(ScriptableRenderContext context) {
			if (independentCMDRequests.Count == 0) return;
			
			Debug.Log("Enter " + Time.frameCount);
			
			foreach (var pair in independentCMDRequests) context.ExecuteCommandBuffer(pair.Key);
			
			context.Submit();

			foreach (var pair in independentCMDRequests) {
				pair.Key.Release();
				pair.Value();
			}
			
			independentCMDRequests.Clear();
		}

		#endregion

		public AdvancedRenderPipeline(AdvancedRenderPipelineSettings settings) {
			instance = this;
			AdvancedRenderPipeline.settings = settings;
			ReversedZ = SystemInfo.usesReversedZBuffer;
			GraphicsSettings.lightsUseLinearIntensity = true;
			GraphicsSettings.useScriptableRenderPipelineBatching = settings.enableSRPBatching;
		}

		protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
			
			ExecuteIndependentCommandBufferRequest(context);
			
			RequestCameraCheck();

			var screenWidth = Screen.width;
			var screenHeight = Screen.height;

			foreach (var camera in cameras) {
				var cameraRenderer = GetCameraRenderer(camera);
				// Debug.Log(Time.frameCount + " " + camera.name);
				cameraRenderer.PreUpdate();
				cameraRenderer.SetResolutionAndRatio(screenWidth, screenHeight, 1f, 1f);
				cameraRenderer.Render(context);
			}
		}

		protected override void Dispose(bool disposing) {
			foreach (var renderer in cameraRenderers.Values) renderer.Dispose();
			cameraRenderers.Clear();
			tempCameras.Clear();
			base.Dispose(disposing);
		}

		internal CameraRenderer GetCameraRenderer(Camera camera) {
			if (!cameraRenderers.TryGetValue(camera, out var renderer)) {
				renderer = CameraRenderer.CreateCameraRenderer(camera,
					CameraRenderer.DefaultToAdvancedCameraType(camera.cameraType));
				cameraRenderers.Add(camera, renderer);
			}

			return renderer;
		}
	}
}
