using System;
using UnityEngine;

namespace RP_Tests {
    [ExecuteInEditMode]
    public class RotaterWS : MonoBehaviour {

        // public bool executeInEdit;
        public float speed = 1f;
        public Vector3 rotationAxis = Vector3.up;

        private void Update() {
            transform.Rotate(rotationAxis, speed * Time.deltaTime, Space.World);
        }
    }
}