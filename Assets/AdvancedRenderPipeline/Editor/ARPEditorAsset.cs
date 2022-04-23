using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace AdvancedRenderPipeline.Editor {
	[CreateAssetMenu(fileName = "ARP Editor Asset", menuName = "Advanced Render Pipeline/ARP Editor Asset", order = 0)]
	public class ARPEditorAsset : ScriptableObject {
		[Range(128, 1024)]
		public int iblLutResolution = 1024;
		public bool separateLuts;
		public GraphicsFormat iblLutFormat = GraphicsFormat.R16G16B16A16_UNorm;
		public GraphicsFormat diffuseLutFormat = GraphicsFormat.R16G16_UNorm;
		public GraphicsFormat specularLutFormat = GraphicsFormat.R16G16_UNorm;
		public bool displayLutRefereces;
		public Texture referenceLut1;
		public Texture referenceLut2;
		public ComputeShader iblLutGenerationShader;
		
		[MenuItem("Advanced RP/System/Log System Info")]
		public static void LogSystemInfo() {
			Debug.Log("Uses Reversed ZBuffer: " + SystemInfo.usesReversedZBuffer);
			Debug.Log("Supports Instancing: " + SystemInfo.supportsInstancing);
			Debug.Log("Supports Async Compute: " + SystemInfo.supportsAsyncCompute);
			Debug.Log("Supports Conservative Raster: " + SystemInfo.supportsConservativeRaster);
			Debug.Log("Supports Geometry Shaders: " + SystemInfo.supportsGeometryShaders);
			Debug.Log("Supports Compute Shaders: " + SystemInfo.supportsComputeShaders);
			Debug.Log("Supports Tessellation Shaders: " + SystemInfo.supportsTessellationShaders);
			Debug.Log("Supports Graphics Fence: " + SystemInfo.supportsGraphicsFence);
			Debug.Log("Copy Texture Support: " + SystemInfo.copyTextureSupport);
			Debug.Log("Supports Vibration: " + SystemInfo.supportsVibration);
		}

		public static bool AssetExistsAt(string path) {
			var guid = AssetDatabase.AssetPathToGUID(path);
			return !string.IsNullOrEmpty(guid);
		}

		public static void CreateOrOverrideAssetAt(Object asset, string path) {
			/*
			if (AssetExistsAt(path)) {
				Debug.Log(path + " Exists!");
				AssetDatabase.DeleteAsset(path);
			}
			*/
            
			AssetDatabase.DeleteAsset(path);
			AssetDatabase.CreateAsset(asset, path);
		}

		public static void CreateAssetAt(Object asset, string path, bool overrideExistingAsset = false) {
			if (overrideExistingAsset) {
				CreateOrOverrideAssetAt(asset, path);
				return;
			}

			path = AssetDatabase.GenerateUniqueAssetPath(path);
			AssetDatabase.CreateAsset(asset, path);
		}
	}
}