using System;
using UnityEngine;

namespace RP_Tests {
    public class MoveBetween : MonoBehaviour {
        public bool loop = true;
        [Min(.0001f)]
        public float interval = 1.0f;
        public Transform end;
        public Vector3 endPosition;
        public bool freezeX;
        public bool freezeY;
        public bool freezeZ;

        private Vector3 from;
        private float startTime;

        private void Awake() => Setup();

        private void Update() {
            var t = Time.time - startTime;
            var target = Vector3.Lerp(from, endPosition, (loop ? Mathf.PingPong(t, interval) : t) / interval);
            var curr = transform.position;
            if (freezeX) target.x = curr.x;
            if (freezeY) target.y = curr.y;
            if (freezeZ) target.z = curr.z;

            transform.position = target;
        }

        private void OnValidate() => Setup();

        private void Setup() {
            from = transform.position;
            if (end != null) endPosition = end.position;

            startTime = Time.time;
        }
    }
}