using UnityEngine;

namespace RP_Tests {
    public class Translator : MonoBehaviour {
        public float speed = 1f;
        public Vector3 direction;

        private void Update() {
            if (direction != Vector3.zero) transform.position += direction * speed * Time.deltaTime;
        }
    }
}