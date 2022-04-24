using System;
using System.Diagnostics;
using AdvancedRenderPipeline.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RP_Tests {
	[ExecuteInEditMode]
	public class PBRShowcase : MonoBehaviour {

		public bool refreshToggle;
		[Min(1)]
		public int num = 11;
		public float interval = 1.2f;
		public Vector3 offset;
		public Vector3 scale = Vector3.one;
		public Vector3 rotation = Vector3.zero;
		public Mesh mesh;
		public Material mat;

		public Color color;
		public bool customizedInput;
		public bool  changeMetallic;
		public float defaultMetallic;
		public float endMetallic;
		public bool  changeSmoothness;
		public float defaultSmoothness;
		public float endSmoothness;
		public float[] metallicValues;
		public float[] smoothnessValues;

		private MaterialPropertyBlock _mpb;
		private Matrix4x4[] _matrices;

		public void Awake() => Setup();

		public void Update() => Graphics.DrawMeshInstanced(mesh, 0, mat, _matrices, num, _mpb);

		private void Setup() {
			Vector4[] _colorValues = new Vector4[num];
			float[] _metallicValues = new float[num];
			float[] _smoothnessValues = new float[num];
			_matrices = new Matrix4x4[num];

			int mid = num / 2;

			var quat = Quaternion.Euler(rotation);

			for (int i = 0; i < num; i++) {
				var pos = transform.position + offset;
				pos.x += (i - mid) * interval;
				_matrices[i] = Matrix4x4.TRS(pos, quat, scale);
				_colorValues[i] = color.linear;
				_metallicValues[i] = changeMetallic ? ( customizedInput ? metallicValues[i] : Mathf.Lerp(defaultMetallic, endMetallic, i / (num - 1f)) ) : defaultMetallic;
				_smoothnessValues[i] = changeSmoothness ? Mathf.Lerp(defaultSmoothness, endSmoothness, i / (num - 1f)) : defaultSmoothness;
				_smoothnessValues[i] = changeSmoothness ? ( customizedInput ? smoothnessValues[i] : Mathf.Lerp(defaultSmoothness, endSmoothness, i / (num - 1f)) ) : defaultSmoothness;
			}
			
			_mpb = new MaterialPropertyBlock();
			_mpb.SetVectorArray("_AlbedoTint", _colorValues);
			_mpb.SetFloatArray("_MetallicScale", _metallicValues);
			_mpb.SetFloatArray("_SmoothnessScale", _smoothnessValues);
		}

		private void OnValidate() => Setup();

#if UNITY_EDITOR
		[Conditional("UNITY_EDITOR")]
		private void OnDrawGizmosSelected() {

			if (!enabled) return;
			
			GUIStyle redStyle = new GUIStyle {
				normal = {
					textColor = Color.red
				}
			};
			
			GUIStyle greenStyle = new GUIStyle {
				normal = {
					textColor = Color.green
				}
			};

			var ms = _mpb.GetFloatArray("_MetallicScale");
			var ss = _mpb.GetFloatArray("_SmoothnessScale");
			
			for (var i = 0; i < _matrices.Length; i++) {
				UnityEditor.Handles.Label(_matrices[i].GetPosition() + new Vector3(-.5f, 1f, .0f), "M: " + ms[i], greenStyle);
				UnityEditor.Handles.Label(_matrices[i].GetPosition() + new Vector3(-.5f, 1.25f, .0f), "S: " + ss[i], greenStyle);
			}
		}
#endif
	}
}