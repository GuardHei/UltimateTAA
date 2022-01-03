using System;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace AdvancedRenderPipeline.Editor {
	
	[CustomEditor(typeof(ARPEditorAsset))]
	public class ARPEditorAssetEditor : UnityEditor.Editor {

		private static RenderTexture lut;
		private static RenderTexture diffuseLut;
		private static RenderTexture specularLut;
		private static string errorText = "";
		
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			if (Runtime.AdvancedRenderPipeline.instance == null) return;
			
			ARPEditorAsset asset = target as ARPEditorAsset;

			if (asset == null) return;
			
			GUILayout.Space(20);
			
			GUILayout.BeginHorizontal();

			if (!asset.separateLuts && lut && lut.IsCreated() && lut.isReadable && !lut.IsDestroyed()) {
				if (!AssetDatabase.Contains(lut)) {
					if (GUILayout.Button("Save IBL Lut")) {
						var path = EditorUtility.SaveFilePanelInProject("Save IBL BRDF Lut", "IBL BRDF Lut.png", "png", "Select a location to save", "Assets/AdvancedRenderPipeline/Profiles/");
						if (!string.IsNullOrEmpty(path)) {
							Texture2D save = new Texture2D(lut.width, lut.height);
							var temp = RenderTexture.active;
							RenderTexture.active = lut;
							save.ReadPixels(new Rect(0, 0, lut.width, lut.height), 0, 0);
							RenderTexture.active = temp;
							byte[] data = save.EncodeToPNG();
							File.WriteAllBytes(Path.GetFullPath(path), data);
							// AssetDatabase.CreateAsset(lut, path);
							AssetDatabase.ImportAsset(path);
							Debug.Log("IBL Lut Saved");
							errorText = "";
						}
					} else if (GUILayout.Button("Clear IBL Lut Cache")) {
						lut.Release();
						lut = null;
						errorText = "";
					}
				}
			} else if (asset.separateLuts) {
				GUILayout.BeginVertical();
				
				if (diffuseLut && diffuseLut.IsCreated() && diffuseLut.isReadable && !diffuseLut.IsDestroyed()) {
					if (!AssetDatabase.Contains(diffuseLut)) {
						if (GUILayout.Button("Save Diffuse Lut")) {
							var path = EditorUtility.SaveFilePanelInProject("Save Diffuse IBL BRDF Lut", "Diffuse IBL BRDF Lut.png", "png", "Select a location to save", "Assets/AdvancedRenderPipeline/Profiles/");
							if (!string.IsNullOrEmpty(path)) {
								Texture2D save = new Texture2D(diffuseLut.width, diffuseLut.height);
								var temp = RenderTexture.active;
								RenderTexture.active = diffuseLut;
								save.ReadPixels(new Rect(0, 0, diffuseLut.width, diffuseLut.height), 0, 0);
								RenderTexture.active = temp;
								byte[] data = save.EncodeToPNG();
								File.WriteAllBytes(Path.GetFullPath(path), data);
								AssetDatabase.ImportAsset(path);
								Debug.Log("Diffuse Lut Saved");
								errorText = "";
							}
						} else if (GUILayout.Button("Clear Diffuse Lut Cache")) {
							diffuseLut.Release();
							diffuseLut = null;
							errorText = "";
						}
					}
				}
				
				GUILayout.EndVertical();
				GUILayout.BeginVertical();
				
				if (specularLut && specularLut.IsCreated() && specularLut.isReadable && !specularLut.IsDestroyed()) {
					if (!AssetDatabase.Contains(specularLut)) {
						if (GUILayout.Button("Save Specular Lut")) {
							var path = EditorUtility.SaveFilePanelInProject("Save Specular IBL BRDF Lut", "Specular IBL BRDF Lut.png", "png", "Select a location to save", "Assets/AdvancedRenderPipeline/Profiles/");
							if (!string.IsNullOrEmpty(path)) {
								Texture2D save = new Texture2D(specularLut.width, specularLut.height);
								var temp = RenderTexture.active;
								RenderTexture.active = specularLut;
								save.ReadPixels(new Rect(0, 0, specularLut.width, specularLut.height), 0, 0);
								RenderTexture.active = temp;
								byte[] data = save.EncodeToPNG();
								File.WriteAllBytes(Path.GetFullPath(path), data);
								AssetDatabase.ImportAsset(path);
								Debug.Log("Specular Lut Saved");
								errorText = "";
							}
						} else if (GUILayout.Button("Clear Specular Lut Cache")) {
							specularLut.Release();
							specularLut = null;
							errorText = "";
						}
					}
				}
				
				GUILayout.EndVertical();
			}

			bool iblLutError = false;
			if (GUILayout.Button("Generate IBL Lut(s)")) {
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
				} else if (!asset.separateLuts) {
					var lutShader = asset.iblLutGenerationShader;
					int kernel = lutShader.FindKernel("GenerateIBLLut");

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
					
					lutShader.SetFloat("_Width", asset.iblLutResolution);
					lutShader.SetFloat("_Height", asset.iblLutResolution);
					lutShader.SetTexture(kernel, "_ResultLut", lut);
					
					Debug.Log("Start generating IBL Lut");
					
					errorText = "";

					try {
						lutShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);
					} catch (Exception e) {
						lut.Release();
						lut = null;
						iblLutError = true;
						errorText = e.Message;
					}
					
					Debug.Log("Finish generating IBL Lut");
				} else if (asset.separateLuts) {
					var lutShader = asset.iblLutGenerationShader;
					int kernel = lutShader.FindKernel("GenerateSeparateIBLLuts");
					
					if (diffuseLut != null) {
						diffuseLut.Release();
						diffuseLut = null;
					}
					
					if (specularLut != null) {
						specularLut.Release();
						specularLut = null;
					}

					var diffuseDesc = new RenderTextureDescriptor(asset.iblLutResolution, asset.iblLutResolution, asset.diffuseLutFormat, 0) {
						enableRandomWrite = true,
						useMipMap = false,
						sRGB = false
					};

					diffuseLut = new RenderTexture(diffuseDesc);
					diffuseLut.Create();
					
					var specularDesc = new RenderTextureDescriptor(asset.iblLutResolution, asset.iblLutResolution, asset.specularLutFormat, 0) {
						enableRandomWrite = true,
						useMipMap = false,
						sRGB = false
					};

					specularLut = new RenderTexture(specularDesc);
					specularLut.Create();
					
					int threadGroupX = Mathf.CeilToInt(asset.iblLutResolution / 8f);
					int threadGroupY = Mathf.CeilToInt(asset.iblLutResolution / 8f);
					
					lutShader.SetFloat("_Width", asset.iblLutResolution);
					lutShader.SetFloat("_Height", asset.iblLutResolution);
					lutShader.SetTexture(kernel, "_DiffuseLut", diffuseLut);
					lutShader.SetTexture(kernel, "_SpecularLut", specularLut);
					
					Debug.Log("Start generating separate IBL Luts");
					
					errorText = "";

					try {
						lutShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);
					} catch (Exception e) {
						lut.Release();
						lut = null;
						iblLutError = true;
						errorText = e.Message;
					}
					
					Debug.Log("Finish generating separate IBL Luts");
				}
			}
			
			GUILayout.EndHorizontal();

			if (iblLutError || !string.IsNullOrEmpty(errorText)) EditorGUILayout.HelpBox(errorText, MessageType.Error);
			else {
				float height = 320;
				if (!asset.separateLuts && lut && lut.IsCreated() && lut.isReadable && !lut.IsDestroyed()) {
					GUILayout.Space(20);
					if (asset != null && (asset.referenceLut1 != null || asset.referenceLut2 != null) && asset.displayLutRefereces) {
						GUILayout.BeginVertical();
						if (asset.referenceLut1 != null) {
							GUILayout.Label(asset.referenceLut1, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
							GUILayout.Space(10);
						}
					
						GUILayout.Label(lut, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });

						if (asset.referenceLut2 != null) {
							GUILayout.Label(asset.referenceLut2, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
						}
						
						GUILayout.EndVertical();
					} else {
						GUILayout.Label(lut, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
					}
				} else if (asset.separateLuts) {
					GUILayout.Space(20);
					GUILayout.BeginVertical();
					
					if (diffuseLut && diffuseLut.IsCreated() && diffuseLut.isReadable && !diffuseLut.IsDestroyed()) {
						GUILayout.Label(diffuseLut, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
						GUILayout.Space(10);
					}
					
					if (specularLut && specularLut.IsCreated() && specularLut.isReadable && !specularLut.IsDestroyed()) {
						GUILayout.Label(specularLut, new GUIStyle { fixedHeight = height, stretchHeight = true, stretchWidth = true, alignment = TextAnchor.MiddleCenter });
					}
					
					GUILayout.EndVertical();
				}
			}
		}
	}
}