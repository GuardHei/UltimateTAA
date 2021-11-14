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

		private Matrix4x4[] _matrices = new Matrix4x4[NUM];
		private Vector4[] _baseColors = new Vector4[NUM];

		private MaterialPropertyBlock _mpb;

		private void Awake() {
			for (int i = 0; i < NUM; i++) {
				_matrices[i] = Matrix4x4.TRS(transform.position + Random.insideUnitSphere * radius, Quaternion.identity, Vector3.one);
				_baseColors[i] = new Vector4(Random.value, Random.value, Random.value, 1f);
			}

			_mpb = new MaterialPropertyBlock();
			_mpb.SetVectorArray(colorKeyWord, _baseColors);
		}
		
		private void Update() {
			Graphics.DrawMeshInstanced(mesh, 0, mat, _matrices, NUM, _mpb);
		}
	}
}