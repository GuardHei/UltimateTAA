using System;
using UnityEngine;

namespace RP_Tests {
	[ExecuteInEditMode]
	public class PBRShowcase : MonoBehaviour {
		
		[Min(1)]
		public int num = 11;
		public float interval = 1.2f;
		public Vector3 offset;
		public Vector3 scale = Vector3.one;
		public Mesh mesh;
		public Material mat;

		public bool changeMetallic;
		public float defaultMetallic;
		public bool changeSmoothness;
		public float defaultSmoothness;

		private MaterialPropertyBlock _mpb;
		private Matrix4x4[] _matrices;

		public void Awake() => Setup();

		public void Update() {
			Graphics.DrawMeshInstanced(mesh, 0, mat, _matrices, num, _mpb);
		}

		private void Setup() {
			float[] _metallicValues = new float[num];
			float[] _smoothnessValues = new float[num];
			_matrices = new Matrix4x4[num];

			int mid = num / 2;

			for (int i = 0; i < num; i++) {
				var pos = transform.position + offset;
				pos.x = (i - mid) * interval;
				_matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, scale);
				_metallicValues[i] = changeMetallic ? Mathf.Lerp(0f, 1f, 1f - i / (num - 1f)) : defaultMetallic;
				_smoothnessValues[i] = changeSmoothness ? Mathf.Lerp(0f, 1f, 1f - i / (num - 1f)) : defaultSmoothness;
			}
			
			_mpb = new MaterialPropertyBlock();
			_mpb.SetFloatArray("_MetallicScale", _metallicValues);
			_mpb.SetFloatArray("_SmoothnessScale", _smoothnessValues);
		}

		private void OnValidate() => Setup();
	}
}