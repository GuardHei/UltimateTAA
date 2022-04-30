using System;
using UnityEngine;

namespace RP_Tests {
    [ExecuteInEditMode]
    public class ShadowDebugger : MonoBehaviour {

        public bool onlyShowWhenSelected;
        public bool drawBBoxes;
        public bool drawEntireFrustum;
        public bool drawFrustums;
        public ShadowmapManager sm;

        public void OnDrawGizmos() {
            if (onlyShowWhenSelected) return;
            DrawDebug();
        }

        public void OnDrawGizmosSelected() {
            if (!onlyShowWhenSelected) return;
            DrawDebug();
        }

        internal void DrawDebug() {
            if (sm == null) {
                sm = new ShadowmapManager();
                return;
            }

            var mainLight = RenderSettings.sun;
            
            sm.UpdateCSM(Camera.main, mainLight.transform.rotation * Vector3.forward);
            
            if (drawBBoxes) sm.DrawBBoxes();
            if (drawEntireFrustum) sm.DrawEntireFrustum();
            if (drawFrustums) sm.DrawFrustums();
        }

        public void OnDestroy() {
            sm?.Dispose();
        }
    }
}