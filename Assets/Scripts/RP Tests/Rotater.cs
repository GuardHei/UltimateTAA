using System;
using UnityEngine;

namespace RP_Tests {
    public class Rotater : MonoBehaviour {

        public float speed = 1f;
        public bool selfRotation;
        public Transform rotationCenter;
        public Vector3 rotationPoint;
        public Vector3 rotationAxis = Vector3.up;

        private void Update() {
            if (!selfRotation) {
                var center = rotationCenter ? rotationCenter.position : rotationPoint;
                transform.RotateAround(center, rotationAxis, speed * Time.deltaTime);
            } else transform.Rotate(rotationAxis, speed * Time.deltaTime);
        }
    }
}