using System;
using UnityEngine;

namespace RP_Tests {
    public class RuntimeDebugger : MonoBehaviour {
        
        private static int startKeyCode = (int) KeyCode.Alpha1;

        public GameObject[] switchableObjects;

        private void Awake() {
            if (!Application.isEditor) Application.targetFrameRate = 60;
        }

        private void Update() {
            var len = Math.Min(switchableObjects?.Length ?? 0, 9);
            for (int i = 0; i < len; i++) {
                if (switchableObjects[i] == null) return;
                if (Input.GetKeyUp((KeyCode) startKeyCode + i)) {
                    var obj = switchableObjects[i];
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }
    }
}
