using System;
using UnityEngine;

namespace RP_Tests {
    [ExecuteInEditMode]
    public class ShadowDebugger : MonoBehaviour {

        // public bool onlyShowWhenSelected;
        public bool drawBBoxes;
        public bool drawEntireFrustum;
        public bool drawFrustums;
        public bool useCustomColors;
        public Color c0;
        public Color c1;
        public Color c2;
        public Color c3;
        public Color color;
        public ShadowmapManager sm;

        public void OnDrawGizmos() {
            // if (onlyShowWhenSelected) return;
            // DrawDebug();
        }

        public void OnDrawGizmosSelected() {
            // if (!onlyShowWhenSelected) return;
            // DrawDebug();
        }

        void Update() => DrawDebug();

        internal void DrawDebug() {
            if (sm == null) {
                sm = new ShadowmapManager();
                return;
            }

            // skip the first n frames
            if (Time.renderedFrameCount <= 2) return;

            var cam = Camera.main;
            if (!cam) return;
            
            sm.UpdateCSM(cam, RenderSettings.sun.transform.rotation * Vector3.forward);
            
            if (useCustomColors) {
                if (drawBBoxes) sm.DrawBBoxes(c0, c1, c2, c3);
                if (drawEntireFrustum) sm.DrawEntireFrustum(color);
                if (drawFrustums) sm.DrawFrustums(c0, c1, c2, c3);
            } else {
                if (drawBBoxes) sm.DrawBBoxes();
                if (drawEntireFrustum) sm.DrawEntireFrustum();
                if (drawFrustums) sm.DrawFrustums();
            }
        }

        public void OnDestroy() {
            sm?.Dispose();
        }

        public void OnDisable() {
            sm?.Dispose();
        }
    }
}