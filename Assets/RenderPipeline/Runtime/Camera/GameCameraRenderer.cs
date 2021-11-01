using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if UNITY_EDITOR
public class GameCameraRenderer : CameraRenderer {
#else
public sealed class GameCameraRenderer : CameraRenderer {
#endif

	#region Pipeline Callbacks

	public event Action beforeCull;
	public event Action beforeFirstPass;
	public event Action beforeTransparent;
	public event Action beforePostProcess;
	public event Action afterLastPass;
	public event Action afterSubmission;	

	#endregion

	internal readonly BufferedRTHandleSystem _historyBuffers = new BufferedRTHandleSystem();

	internal string _rendererDesc;

	#region RT Handles

	internal RTHandle _rawColorTex;
	internal RTHandle _prevRawColorTex;
	internal RTHandle _colorTex;
	internal RTHandle _depthTex;
	internal RTHandle _prevDepthTex;
	internal RTHandle _stencilTex;
	internal RTHandle _prevStencilTex;
	internal RTHandle _velocityTex;
	internal RTHandle _prevVelocityTex;
	internal RTHandle _normalTex;

	#endregion

	public GameCameraRenderer(Camera camera) : base(camera) {
		cameraType = AdvancedCameraType.Game;
		_rendererDesc = "Render Game (" + camera + ")";
		InitBuffers();
	}

	public override void Render(ScriptableRenderContext context) {
		_context = context;
		_cmd = CommandBufferPool.Get(_rendererDesc);

		BeginSample(_rendererDesc);
		
		ResetBuffers();
		GetBuffers();
		
		Setup();

		beforeCull?.Invoke();
		
		Cull();
		
		beforeFirstPass?.Invoke();
		
		DrawDepthStencilPrepass();
		DrawShadowPass();
		DrawOpaqueLightingPass();
		
		beforeTransparent?.Invoke();
		
		DrawTransparentLightingPass();
		DrawSkybox();
		
		beforePostProcess?.Invoke();
		
		afterLastPass?.Invoke();
		
		EndSample(_rendererDesc);
		
		Submit();
		
		afterSubmission?.Invoke();
		
		ReleaseBuffers();
		
		CommandBufferPool.Release(_cmd);
	}

	public override void Setup() {
		_context.SetupCameraProperties(camera);
		
		var clearColor = camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.black;
		
		// SetRenderTarget();
		ClearRenderTarget(RTClearFlags.All, clearColor);
		
		ExecuteCommand(_cmd);
	}

	public void Cull() {
		if (!camera.TryGetCullingParameters(out var cullingParameters)) {
			Debug.Log("Culling Failed for " + _rendererDesc);
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
		var filterSettings = new FilteringSettings(ShaderTagManager.OPAQUE_QUEUE);
		
		_context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
	}

	public void DrawTransparentLightingPass() {
		
	}

	public void DrawSkybox() {
		_context.DrawSkybox(camera);
	}

	public void InitBuffers() {
		ResetBuffers();
		
		_historyBuffers.AllocBuffer(ShaderKeywordManager.RAW_COLOR_TEXTURE, (system, i) => system.Alloc(Ratio, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, filterMode: FilterMode.Bilinear), 2);
		_historyBuffers.AllocBuffer(ShaderKeywordManager.COLOR_TEXTURE, (system, i) => system.Alloc(Ratio, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, filterMode: FilterMode.Bilinear), 1);
		_historyBuffers.AllocBuffer(ShaderKeywordManager.DEPTH_TEXTURE, (system, i) => system.Alloc(Ratio, colorFormat: GraphicsFormat.None, depthBufferBits: DepthBits.Depth32), 2);
		_historyBuffers.AllocBuffer(ShaderKeywordManager.VELOCITY_TEXTURE, (system, i) => system.Alloc(Ratio, colorFormat: GraphicsFormat.R16G16_SNorm), 2);
	}

	public void ResetBuffers() {
		_historyBuffers.SwapAndSetReferenceSize(outputWidth, outputHeight);
	}

	public void GetBuffers() {
		_rawColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.RAW_COLOR_TEXTURE, 0);
		_prevRawColorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.RAW_COLOR_TEXTURE, 1);
		// if (_prevRawColorTex == null) Debug.Log("Not Allocated Before!");
		_colorTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.COLOR_TEXTURE, 0);
		_depthTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DEPTH_TEXTURE, 0);
		_prevDepthTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.DEPTH_TEXTURE, 1);
		_velocityTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.VELOCITY_TEXTURE, 0);
		_prevVelocityTex = _historyBuffers.GetFrameRT(ShaderKeywordManager.VELOCITY_TEXTURE, 1);
	}

	public void ReleaseBuffers() {
		
	}
}
