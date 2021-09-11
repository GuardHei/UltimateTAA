using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Advanced Render Pipeline")]
public class AdvancedRenderPipelineAsset : RenderPipelineAsset {

	protected override RenderPipeline CreatePipeline() => new AdvancedRenderPipeline();
}