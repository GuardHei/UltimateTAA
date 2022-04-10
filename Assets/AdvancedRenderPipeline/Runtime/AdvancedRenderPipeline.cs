using System;
using System.Collections.Generic;
using AdvancedRenderPipeline.Runtime.Cameras;
using UnityEngine;
using UnityEngine.Profiling;
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
				var cam = pair.Value;
				cameraRenderers.Remove(pair.Key);
				cam.Dispose();
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
			RegisterCamera(camera, CameraRenderer.GetCameraType(camera));

		public static bool RegisterCamera(Camera camera, AdvancedCameraType type) {
			if (cameraRenderers.ContainsKey(camera)) return false;
			cameraRenderers.Add(camera, CameraRenderer.CreateCameraRenderer(camera, type));
			return true;

		}

		public static bool AddIndependentCommandBufferRequest(CommandBuffer request, Action onSubmit) {
			return false;
			if (instance == null) return false;
			independentCMDRequests.Add(request, onSubmit);
			return true;
		}

		protected override void ProcessRenderRequests(ScriptableRenderContext context, Camera camera, List<Camera.RenderRequest> renderRequests) {
			base.ProcessRenderRequests(context, camera, renderRequests);
		}

		private static void ExecuteIndependentCommandBufferRequest(ScriptableRenderContext context) {
			if (independentCMDRequests.Count == 0) return;
			
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

		private double tempA, tempB;

		protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
			
			// ExecuteIndependentCommandBufferRequest(context);

			RequestCameraCheck();

			var screenWidth = Screen.width;
			var screenHeight = Screen.height;
			
			BeginFrameRendering(context, cameras);

			foreach (var camera in cameras) {
				var pixelWidth = camera.pixelWidth;
				var pixelHeight = camera.pixelHeight;
				
				var cameraRenderer = GetCameraRenderer(camera);
				cameraRenderer.PreUpdate();
				cameraRenderer.SetResolutionAndRatio(pixelWidth, pixelHeight, 1f, 1f);
				
				BeginCameraRendering(context, camera);
				
				cameraRenderer.Render(context);
				
				EndCameraRendering(context, camera);
				
				cameraRenderer.PostUpdate();
			}
			
			EndFrameRendering(context, cameras);
		}

		protected override void Dispose(bool disposing) {
			foreach (var renderer in cameraRenderers.Values) renderer.Dispose();
			cameraRenderers.Clear();
			tempCameras.Clear();
			base.Dispose(disposing);
		}

		internal CameraRenderer GetCameraRenderer(Camera camera) {
			var cameraType = CameraRenderer.GetCameraType(camera);
			// Debug.Log(camera.name + " " + cameraType);
			if (!cameraRenderers.TryGetValue(camera, out var renderer)) {
				renderer = CameraRenderer.CreateCameraRenderer(camera, cameraType);
				cameraRenderers.Add(camera, renderer);
			} else {
				if (cameraType != renderer.cameraType) {
					var oldRenderer = renderer;
					renderer = CameraRenderer.CreateCameraRenderer(camera, cameraType);
					cameraRenderers[camera] = renderer;
					oldRenderer.Dispose();
				}
			}

			return renderer;
		}
	}
}
