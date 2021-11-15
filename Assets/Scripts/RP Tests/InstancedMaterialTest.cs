using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RP_Tests {
	public class InstancedMaterialTest : MonoBehaviour {

		public const int NUM = 1022;

		[Min(0f)]
		public float radius = 15f;
		public Mesh mesh;
		public Material mat;
		public string colorKeyWord = "_BaseColor";
		public bool isPbr;

		private Matrix4x4[] _matrices = new Matrix4x4[NUM];
		private Vector4[] _baseColors = new Vector4[NUM];
		private float[] _metallicValues = new float[NUM];
		private float[] _smoothnessValues = new float[NUM];

		private MaterialPropertyBlock _mpb;

		private void Awake() {
			for (int i = 0; i < NUM; i++) {
				_matrices[i] = Matrix4x4.TRS(transform.position + Random.insideUnitSphere * radius, Quaternion.identity, Vector3.one);
				_baseColors[i] = new Vector4(Random.value, Random.value, Random.value, 1f);
				_metallicValues[i] = Random.value;
				_smoothnessValues[i] = Random.value;
			}

			_mpb = new MaterialPropertyBlock();
			_mpb.SetVectorArray(colorKeyWord, _baseColors);

			if (isPbr) {
				_mpb.SetFloatArray("_MetallicScale", _metallicValues);
				_mpb.SetFloatArray("_SmoothnessScale", _smoothnessValues);
			}
		}
		
		private void Update() {
			Graphics.DrawMeshInstanced(mesh, 0, mat, _matrices, NUM, _mpb);
		}
	}
}