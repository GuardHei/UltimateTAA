using UnityEngine;

namespace AdvancedRenderPipeline.Runtime.Cameras {
	public class CameraWanderer : MonoBehaviour {

		public float walkSpeed = 3f;
		public float runSpeed = 7f;
		public float sensitivity = .5f;

		public KeyCode fwd = KeyCode.W;
		public KeyCode bwd = KeyCode.S;
		public KeyCode left = KeyCode.A;
		public KeyCode right = KeyCode.D;
		public KeyCode run = KeyCode.LeftShift;
		public KeyCode rotate = KeyCode.Mouse0;
		
		[HideInInspector]
		public Vector3 lastPos;

		private void Start() => lastPos = transform.position;

		private void Update() {
			var speed = Input.GetKey(run) ? runSpeed : walkSpeed;
			if (Input.GetKey(fwd)) transform.Translate(Vector3.forward * speed * Time.deltaTime);
			if (Input.GetKey(bwd)) transform.Translate(Vector3.back * speed * Time.deltaTime);
			if (Input.GetKey(left)) transform.Translate(Vector3.left * speed * Time.deltaTime);
			if (Input.GetKey(right)) transform.Translate(Vector3.right * speed * Time.deltaTime);

			if (Input.GetKey(rotate)) {
				var dPos = Input.mousePosition - lastPos;
				transform.Rotate(new Vector3(-dPos.y * sensitivity, dPos.x * sensitivity, 0));
				var x = transform.rotation.eulerAngles.x;
				var y = transform.rotation.eulerAngles.y;
				transform.rotation = Quaternion.Euler(x, y, 0);
			}

			lastPos = Input.mousePosition;
		}
	}
}