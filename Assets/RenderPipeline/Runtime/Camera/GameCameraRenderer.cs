using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class GameCameraRenderer : CameraRenderer {

	public GameCameraRenderer(Camera camera) : base(camera) => cameraType = AdvancedCameraType.Game;
	
	public override void Render(ScriptableRenderContext context) {
		_context = context;
		
		Setup();
		DrawLightingPass();
		DrawSkybox();
	}

	public override void Setup() {
		_context.SetupCameraProperties(camera);
	}

	public void DrawLightingPass() {
		
	}

	public void DrawSkybox() {
		_context.DrawSkybox(camera);
	}
}
