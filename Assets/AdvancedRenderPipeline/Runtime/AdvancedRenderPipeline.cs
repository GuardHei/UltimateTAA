using System;
using System.Collections.Generic;
using AdvancedRenderPipeline.Runtime.Cameras;
using AdvancedRenderPipeline.Runtime.PipelineProcessors;
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

		#region Pipeline Processors

		internal List<PipelineProcessor> _pipelineProcessors = new List<PipelineProcessor>();

		#endregion
		
		public bool IsOnFirstFrame => _frameNum == 1; // default is 0, and we start at 1.
		
		private int _frameNum = 0;

		public AdvancedRenderPipeline(AdvancedRenderPipelineSettings settings) {
			instance = this;
			AdvancedRenderPipeline.settings = settings;
			ReversedZ = SystemInfo.usesReversedZBuffer;
			GraphicsSettings.lightsUseLinearIntensity = true;
			GraphicsSettings.useScriptableRenderPipelineBatching = settings.enableSRPBatching;
				
			// add pipeline processors
			_pipelineProcessors.Add(new GeneralPipelineProcessor());
			_pipelineProcessors.Add(new DiffuseProbeProcessor());
		}

		protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
			
			// ExecuteIndependentCommandBufferRequest(context);

			RequestCameraCheck();

			var screenWidth = Screen.width;
			var screenHeight = Screen.height;

			_frameNum++;

			BeginFrameRendering(context, cameras);
			
			ExecutePipelineProcessors(context);

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

		internal void ExecutePipelineProcessors(ScriptableRenderContext context) {
			foreach (var processor in _pipelineProcessors) processor.Process(context);
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
		
		protected override void Dispose(bool disposing) {
			foreach (var renderer in cameraRenderers.Values) renderer.Dispose();
			cameraRenderers.Clear();
			tempCameras.Clear();
			foreach (var processor in _pipelineProcessors) processor.Dispose();
			base.Dispose(disposing);
		}
	}
}
