using UnityEngine;

namespace AdvancedRenderPipeline.Editor {
	[CreateAssetMenu(fileName = "ARP Editor Asset", menuName = "Advanced Render Pipeline/ARP Editor Asset", order = 0)]
	public class ARPEditorAsset : ScriptableObject {
		[Range(256, 1024)]
		public int iblLutResolution = 512;
		public ComputeShader iblLutGenerationShader;
	}
}