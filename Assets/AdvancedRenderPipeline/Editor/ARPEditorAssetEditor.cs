using AdvancedRenderPipeline.Runtime;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Editor {
	
	[CustomEditor(typeof(ARPEditorAsset))]
	public class ARPEditorAssetEditor : UnityEditor.Editor {

		private static RenderTexture lut;
		
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			if (Runtime.AdvancedRenderPipeline.instance == null) return;

			if (lut && lut.IsCreated() && lut.isReadable && !lut.IsDestroyed()) {
				GUILayout.Label(lut);
				if (GUILayout.Button("Clear IBL Lut Cache")) {
					lut.Release();
					lut = null;
				}
			}

			bool iblLutError = false;
			string errorText = "";
			if (GUILayout.Button("Generate IBL Lut")) {
				ARPEditorAsset asset = target as ARPEditorAsset;
				if (asset == null) {
					iblLutError = true;
					errorText = "ARP Editor Asset cannot be null!";
				} else if (asset.iblLutResolution < 128) {
					iblLutError = true;
					errorText = "IBL Lut Resolution cannot be smaller than 128!";
				} else if (asset.iblLutResolution > 1024) {
					iblLutError = true;
					errorText = "IBL Lut Resolution cannot be larger than 1024!";
				} else if (asset.iblLutGenerationShader == null) {
					iblLutError = true;
					errorText = "IBL Lut Generation Shader cannot be null!";
				} else {
					var shader = asset.iblLutGenerationShader;
					int kernel = shader.FindKernel("GenerateIBLLut");

					if (lut != null) {
						lut.Release();
						lut = null;
					}

					RenderTextureDescriptor desc = new RenderTextureDescriptor(asset.iblLutResolution, asset.iblLutResolution, GraphicsFormat.R32G32B32A32_SFloat, 32);
					desc.enableRandomWrite = true;
					desc.useMipMap = false;
					desc.sRGB = false;
					
					// lut = new RenderTexture(asset.iblLutResolution, asset.iblLutResolution, GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormat.D32_SFloat);
					// lut.useMipMap = false;
					// lut.enableRandomWrite = true;

					lut = new RenderTexture(desc);
					
					lut.Create();
					
					int threadGroupX = Mathf.CeilToInt(asset.iblLutResolution / 8f);
					int threadGroupY = Mathf.CeilToInt(asset.iblLutResolution / 8f);

					/*
					CommandBuffer cmd = new CommandBuffer();
					cmd.name = "Generate IBL Lut";

					cmd.SetComputeFloatParam(shader, "_Width", asset.iblLutResolution);
					cmd.SetComputeFloatParam(shader, "_Height", asset.iblLutResolution);
					cmd.SetComputeTextureParam(shader, kernel, "Result", lut);
					
					cmd.DispatchCompute(shader, kernel, threadGroupX, threadGroupY, 1);

					if (!Runtime.AdvancedRenderPipeline.AddIndependentCommandBufferRequest(cmd, OnIBLLutGenerated)) {
						iblLutError = true;
						errorText = "Failed to add independent command buffer request";
						lut.Release();
						lut = null;
					} else {
						Debug.Log("Start generating IBL Lut");
					}
					*/
					
					shader.SetFloat("_Width", asset.iblLutResolution);
					shader.SetFloat("_Height", asset.iblLutResolution);
					shader.SetTexture(kernel, "_ResultLut", lut);
					shader.Dispatch(kernel, threadGroupX, threadGroupY, 1);
					
					Debug.Log("Start generating IBL Lut");
					
					OnIBLLutGenerated();
				}
			}

			if (iblLutError) {
				EditorGUILayout.HelpBox(errorText, MessageType.Error);
			}
		}

		private void OnIBLLutGenerated() {
			if (lut == null) {
				EditorUtility.DisplayDialog("IBL Lut Generation Error", "LUT cannot be null!", "ok");
				return;
			}
			
			// AssetDatabase.CreateAsset(lut, "Assets/TestLut.renderTexture");
		}
	}
}