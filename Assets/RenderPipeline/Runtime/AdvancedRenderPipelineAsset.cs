using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Advanced Render Pipeline/ARP Asset")]
public class AdvancedRenderPipelineAsset : RenderPipelineAsset {

	public AdvancedRenderPipelineSettings settings;

	protected override RenderPipeline CreatePipeline() => new AdvancedRenderPipeline(settings);
}

[Serializable]
public class AdvancedRenderPipelineSettings {

	[Header("Editor")]
	public bool enableDebugView;
	[Header("Batch Settings")]
	public bool enableDynamicBatcing;
	public bool enableAutoInstancing = true;
	public bool enableSRPBatching = true;
	[Header("Display")]
	public bool enableHdr;
	[Header("Transparency")]
	public float alphaTestDepthCutOff = .001f;
	
}