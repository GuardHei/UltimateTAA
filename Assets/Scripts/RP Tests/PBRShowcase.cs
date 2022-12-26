using System;
using System.Diagnostics;
using AdvancedRenderPipeline.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace RP_Tests {
	[ExecuteInEditMode]
	public class PBRShowcase : MonoBehaviour {

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
		public bool changeMetallic;
		public float defaultMetallic;
		public float endMetallic;
		public bool changeSmoothness;
		public float defaultSmoothness;
		public float endSmoothness;
		public float[] metallicValues;
		public float[] smoothnessValues;

		private MaterialPropertyBlock _mpb;
		private Matrix4x4[] _matrices;
		private Vector3[] _positions;
		private Vector4[] _colorValues;
		private float[] _metallicValues;
		private float[] _smoothnessValues;
		
		private MeshRenderer[] _renderers;
		private bool wasPlaying;

		public void Awake() {
			Setup();
			SpawnSpheres();
			UpdateMbp();
		}

		public void Update() {
			if (!EditorApplication.isPlaying) {
				Graphics.DrawMeshInstanced(mesh, 0, mat, _matrices, num, _mpb);
				wasPlaying = false;
			} else {
				if (!wasPlaying) {
					SpawnSpheres();
					UpdateMbp();
				}
				wasPlaying = true;
			}
		}

		private void Setup() {
			_colorValues = new Vector4[num];
			_metallicValues = new float[num];
			_smoothnessValues = new float[num];
			_matrices = new Matrix4x4[num];
			_positions = new Vector3[num];

			int mid = num / 2;

			var quat = Quaternion.Euler(rotation);

			for (var i = 0; i < num; i++) {
				var pos = offset;
				pos.x += (i - mid) * interval;
				_positions[i] = pos;
				pos += transform.position;
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
		
		private void SpawnSpheres() {
			if (!EditorApplication.isPlaying) return;
			_renderers = new MeshRenderer[num];
			// var prototype = new GameObject("PBR Sphere");
			for (var i = 0; i < num; i++) {
				
				var sphere = new GameObject("PBR Sphere " + i) {
					transform = {
						parent = transform,
						localPosition = _positions[i],
						localRotation = Quaternion.Euler(rotation),
						localScale = scale
					}
				};
				

				/*
				var sphere = Instantiate(prototype, transform);
				sphere.transform.localPosition = _positions[i];
				sphere.transform.localRotation = Quaternion.Euler(rotation);
				sphere.transform.localScale = scale;
				*/

				sphere.AddComponent<MeshFilter>().sharedMesh = mesh;
				var r = sphere.AddComponent<MeshRenderer>();
				_renderers[i] = r;
				r.material = mat;
			}
			
			// Destroy(prototype);
		}

		private void UpdateMbp() {
			if (!EditorApplication.isPlaying) return;
			if (_renderers == null || _renderers.Length != num) return;
			for (var i = 0; i < num; i++) {
				var mpb = new MaterialPropertyBlock();
				mpb.SetVector("_AlbedoTint", _colorValues[i]);
				mpb.SetFloat("_MetallicScale", _metallicValues[i]);
				mpb.SetFloat("_SmoothnessScale", _smoothnessValues[i]);
				_renderers[i].SetPropertyBlock(mpb);
			}
		}

		private void OnValidate() {
			Setup();
			UpdateMbp();
		}

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
				Handles.Label(_matrices[i].GetPosition() + new Vector3(-.5f, 1f, .0f), "M: " + ms[i], greenStyle);
				Handles.Label(_matrices[i].GetPosition() + new Vector3(-.5f, 1.25f, .0f), "S: " + ss[i], greenStyle);
			}
		}
#endif
	}
}