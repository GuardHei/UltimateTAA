using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace AdvancedRenderPipeline.Editor {
	
	[CustomEditor(typeof(ARPEditorAsset))]
	public class ARPEditorAssetEditor : UnityEditor.Editor {

		private static RenderTexture lut;
		private static string errorText = "";
		
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			if (Runtime.AdvancedRenderPipeline.instance == null) return;
			
			GUILayout.Space(20);
			
			GUILayout.BeginHorizontal();

			if (lut && lut.IsCreated() && lut.isReadable && !lut.IsDestroyed()) {
				if (!AssetDatabase.Contains(lut)) {
					if (GUILayout.Button("Save IBL Lut")) {
						var path = EditorUtility.SaveFilePanelInProject("Save IBL BRDF Lut", "IBL BRDF Lut.renderTexture", "renderTexture", "Select a location to save");
						if (string.IsNullOrEmpty(path)) path = "Assets/IBL BRDF Lut.renderTexture";
						AssetDatabase.CreateAsset(lut, path);
						errorText = "";
					} else if (GUILayout.Button("Clear IBL Lut Cache")) {
						lut.Release();
						lut = null;
						errorText = "";
					}
				}
			}
			
			ARPEditorAsset asset = target as ARPEditorAsset;

			bool iblLutError = false;
			if (GUILayout.Button("Generate IBL Lut")) {
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

					var desc = new RenderTextureDescriptor(asset.iblLutResolution, asset.iblLutResolution, asset.iblLutFormat, 0) {
						enableRandomWrite = true,
						useMipMap = false,
						sRGB = false
					};

					lut = new RenderTexture(desc);
					lut.Create();
					
					int threadGroupX = Mathf.CeilToInt(asset.iblLutResolution / 8f);
					int threadGroupY = Mathf.CeilToInt(asset.iblLutResolution / 8f);
					
					shader.SetFloat("_Width", asset.iblLutResolution);
					shader.SetFloat("_Height", asset.iblLutResolution);
					shader.SetTexture(kernel, "_ResultLut", lut);
					
					Debug.Log("Start generating IBL Lut");
					
					errorText = "";

					try {
						shader.Dispatch(kernel, threadGroupX, threadGroupY, 1);
					} catch (Exception e) {
						lut.Release();
						lut = null;
						iblLutError = true;
						errorText = e.Message;
					}
					
					Debug.Log("Finish generating IBL Lut");
				}
			}
			
			GUILayout.EndHorizontal();

			if (iblLutError || !string.IsNullOrEmpty(errorText)) EditorGUILayout.HelpBox(errorText, MessageType.Error);
			else if (lut && lut.IsCreated() && lut.isReadable && !lut.IsDestroyed()) {
				float height = 320;
				GUILayout.Space(20);
				if (asset != null && (asset.referenceLut1 != null || asset.referenceLut2 != null)) {
					GUILayout.BeginVertical();
					if (asset.referenceLut1 != null) {
						GUILayout.Label(asset.referenceLut1, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
						GUILayout.Space(10);
					}
					
					GUILayout.Label(lut, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
					GUILayout.EndVertical();
					
					if (asset.referenceLut2 != null) {
						GUILayout.Label(asset.referenceLut2, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
						GUILayout.Space(10);
					}
				} else {
					GUILayout.Label(lut, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
				}
			}
		}
	}
}