using UnityEngine;

namespace RP_Tests {
	public class InstancedMaterialTest : MonoBehaviour {

		public Mesh mesh;
		public Material mat;

		private Matrix4x4[] _matrices = new Matrix4x4[1022];
		private Vector4[] _baseColors = new Vector4[1022];

		private MaterialPropertyBlock _mpb;

		private void Awake() {
			for (int i = 0; i < _matrices.Length; i++) {
				_matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 15f, Quaternion.identity, Vector3.one);
				_baseColors[i] = new Vector4(Random.value, Random.value, Random.value, 1f);
			}
			
			_mpb = new MaterialPropertyBlock();
			_mpb.SetVectorArray("_BaseColor", _baseColors);
		}

		private void Update() {
			Graphics.DrawMeshInstanced(mesh, 0, mat, _matrices, 1022, _mpb);
		}
	}
}