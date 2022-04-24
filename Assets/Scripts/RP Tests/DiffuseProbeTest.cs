using System;
using System.Collections.Generic;
using System.Diagnostics;
using AdvancedRenderPipeline.Runtime;
using AdvancedRenderPipeline.Runtime.PipelineProcessors;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HandleUtility = UnityEngine.ProBuilder.HandleUtility;

namespace RP_Tests {
    [ExecuteInEditMode]
    public class DiffuseProbeTest : MonoBehaviour {

        public static DiffuseGISettings diffuseGISettings => AdvancedRenderPipeline.Runtime.AdvancedRenderPipeline.settings == null ? new DiffuseGISettings() : AdvancedRenderPipeline.Runtime.AdvancedRenderPipeline.settings.diffuseGISettings;

        public bool NeedsRefresh => _diffuseGISettings.volumeCenter != diffuseGISettings.volumeCenter || _diffuseGISettings.dimensions != diffuseGISettings.dimensions || _diffuseGISettings.maxIntervals != diffuseGISettings.maxIntervals;

        public DiffuseProbeDebugMode debugMode = DiffuseProbeDebugMode.ALBEDO;
        public Mesh gizmosProbeMesh;
        public Material gizmosProbeMat;
        public float gizmosProbeRadius = .3f;
        public Color gizmosProbeColor;

        private MaterialPropertyBlock _mpb;
        private Matrix4x4[] _matrices;
        private DiffuseGISettings _diffuseGISettings;

        private void Awake() {
            _diffuseGISettings = diffuseGISettings;
            SetupProbeGizmos();
        }

        private void Update() {
            if (NeedsRefresh) SetupProbeGizmos();
            DrawProbeGizmos();
        }

        private void OnValidate() => SetupProbeGizmos();

        public void DrawProbeGizmos() {
            if (!gizmosProbeMesh) return;
            Graphics.DrawMeshInstanced(gizmosProbeMesh, 0, gizmosProbeMat, _matrices, _diffuseGISettings.Count, _mpb);
        }

        public void SetupProbeGizmos() {
            _diffuseGISettings = diffuseGISettings;
            _matrices = new Matrix4x4[_diffuseGISettings.Count];
            var colorValues = new Vector4[_matrices.Length];
            var debugModes = new float[_matrices.Length];
            var dimensions = diffuseGISettings.dimensions;
            var maxIntervals = diffuseGISettings.maxIntervals;
            var origin = diffuseGISettings.Min;
            for (var i = 0; i < dimensions.x; i++) {
                for (var j = 0; j < dimensions.y; j++) {
                    for (var k = 0; k < dimensions.z; k++) {
                        var probeIndex = diffuseGISettings.GetProbeIndex1d(new Vector3Int(i, j, k));
                        var pos = origin + new Vector3(i * maxIntervals.x, j * maxIntervals.y, k * maxIntervals.z);
                        _matrices[probeIndex] = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(gizmosProbeRadius, gizmosProbeRadius, gizmosProbeRadius));
                        colorValues[probeIndex] = gizmosProbeColor;
                        debugModes[probeIndex] = (int) debugMode;
                    }
                }
            }

            _mpb = new MaterialPropertyBlock();
            if (colorValues.Length != 0) _mpb.SetVectorArray("_AlbedoTint", colorValues);
            if (debugModes.Length != 0) _mpb.SetFloatArray("_DebugMode", debugModes);
        }
    }
}