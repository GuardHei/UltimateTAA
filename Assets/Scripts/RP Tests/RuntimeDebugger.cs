using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RP_Tests {
    public class RuntimeDebugger : MonoBehaviour {

        [Serializable]
        public class DebugGameObject {
            public KeyCode keyCode;
            public GameObject[] gos;
        }
        
        [Serializable]
        public class DebugScript {
            public KeyCode keyCode;
            public MonoBehaviour[] scripts;
        }
        
        private static int startKeyCode = (int) KeyCode.Alpha1;

        public GameObject graphy;
        public GameObject[] switchableObjects;

        [NonReorderable]
        public DebugGameObject[] debugGameObjects;
        [NonReorderable]
        public DebugScript[] debugScripts;

        private void Awake() {
            if (!Application.isEditor) Application.targetFrameRate = 60;
        }

        private void Update() {

            if (Input.GetKey(KeyCode.G)) {
                if (graphy) {
                    graphy.SetActive(!graphy.activeSelf);
                }
            }

            if (Input.GetKey(KeyCode.F)) Application.targetFrameRate = Application.targetFrameRate == 60 ? -1 : 60;
            
            var len = Math.Min(switchableObjects?.Length ?? 0, 9);
            for (int i = 0; i < len; i++) {
                if (switchableObjects[i] == null) return;
                if (Input.GetKeyUp((KeyCode) startKeyCode + i)) {
                    var obj = switchableObjects[i];
                    obj.SetActive(!obj.activeSelf);
                }
            }

            if (debugGameObjects != null) {
                foreach (var dgo in debugGameObjects) {
                    if (Input.GetKeyUp(dgo.keyCode) && dgo.gos != null) {
                        foreach (var go in dgo.gos) {
                            go.SetActive(!go.activeSelf);
                        }
                    }
                }
            }
            
            if (debugScripts != null) {
                foreach (var ds in debugScripts) {
                    if (Input.GetKeyUp(ds.keyCode) && ds.scripts != null) {
                        foreach (var script in ds.scripts) {
                            script.enabled = !script.enabled;
                        }
                    }
                }
            }
        }
    }
}
