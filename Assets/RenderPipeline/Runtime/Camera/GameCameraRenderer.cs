using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class GameCameraRenderer : CameraRenderer {

	internal readonly BufferedRTHandleSystem _historyBuffers = new BufferedRTHandleSystem();

	internal const string RENDERER_DESC = "Render Game View";
	internal CommandBuffer _cmd;

	public GameCameraRenderer(Camera camera) : base(camera) {
		cameraType = AdvancedCameraType.Game;
		InitBuffers();
	}

	public override void Render(ScriptableRenderContext context) {
		_context = context;
		_cmd = CommandBufferPool.Get(RENDERER_DESC);

		BeginSample();
		
		Setup();
		
		Cull();
		DrawDepthStencilPrepass();
		DrawShadowPass();
		DrawOpaqueLightingPass();
		DrawOpaqueLightingPass();
		DrawSkybox();
		
		EndSample();
		
		Submit();
	}
	
	public void BeginSample() {
#if UNITY_EDITOR
		_cmd.BeginSample(RENDERER_DESC);
#endif
	}

	public void EndSample() {
#if UNITY_EDITOR
		_cmd.EndSample(RENDERER_DESC);
#endif
	}

	public override void Setup() {
		_context.SetupCameraProperties(camera);
	}

	public void Cull() {
		if (!camera.TryGetCullingParameters(out var cullingParameters)) {
			Debug.Log("Culling Failed for " + RENDERER_DESC);
			return;
		}
		
		_cullingResults = _context.Cull(ref cullingParameters);
	}

	public void DrawDepthStencilPrepass() {
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque | SortingCriteria.OptimizeStateChanges | SortingCriteria.QuantizedFrontToBack
		};
		var drawSettings = new DrawingSettings(ShaderTagManager.DEPTH_NORMAL, sortingSettings);
		var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
		
		_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
	}

	public void DrawShadowPass() {
		
	}

	public void DrawOpaqueLightingPass() {
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.OptimizeStateChanges
		};
		var drawSettings = new DrawingSettings(ShaderTagManager.SRP_DEFAULT_UNLIT, sortingSettings);
		var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
		
		_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
	}

	public void DrawTransparentLightingPass() {
		
	}

	public void DrawSkybox() {
		_context.DrawSkybox(camera);
	}

	public void InitBuffers() {
		_historyBuffers.SwapAndSetReferenceSize(outputWidth, outputHeight);
	}

	public void GetBuffers() {
		
	}

	public void ReleaseBuffers() {
		
	}
}
