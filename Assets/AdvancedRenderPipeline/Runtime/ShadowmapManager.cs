using System;
using System.Collections.Generic;
using AdvancedRenderPipeline.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public class ShadowmapManager : IDisposable {

    public static ShadowSettings shadowSettings = AdvancedRenderPipeline.Runtime.AdvancedRenderPipeline.settings.shadowSettings;

    internal Camera _shadowCamera;

    #region Directional Light Shadow Params

    internal float[] _dlOrthoWidths = new float[4];

    internal Vector3[] _dlNearCorners = new Vector3[4];
    internal Vector3[] _dlFarCorners = new Vector3[4];

    internal Vector3[] _dlNearCorners0 = new Vector3[4];
    internal Vector3[] _dlNearCorners1 = new Vector3[4];
    internal Vector3[] _dlNearCorners2 = new Vector3[4];
    internal Vector3[] _dlNearCorners3 = new Vector3[4];
    
    internal Vector3[] _dlFarCorners0 = new Vector3[4];
    internal Vector3[] _dlFarCorners1 = new Vector3[4];
    internal Vector3[] _dlFarCorners2 = new Vector3[4];
    internal Vector3[] _dlFarCorners3 = new Vector3[4];

    internal Vector3[] _dlBBox0 = new Vector3[8];
    internal Vector3[] _dlBBox1 = new Vector3[8];
    internal Vector3[] _dlBBox2 = new Vector3[8];
    internal Vector3[] _dlBBox3 = new Vector3[8];

    #endregion

    public ShadowmapManager() {
        _shadowCamera = new GameObject("Shadow Camera").AddComponent<Camera>();
    }

    internal void ComputeLightSpaceAABB(Vector3 lightDir, Vector3[] near, Vector3[] far, ref Vector3[] bbox) {
        var invW2l = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        var w2l = invW2l.inverse;

        // var nearL = new Vector3[4];
        // var farL = new Vector3[4];
        
        var x = new float[8];
        var y = new float[8];
        var z = new float[8];

        for (var i = 0; i < 4; i++) {
            var n = TransformH(w2l, near[i], 1.0f);
            var f = TransformH(w2l, far[i], 1.0f);
            
            x[i] = n.x;
            x[i + 4] = f.x;
            y[i] = n.y;
            y[i + 4] = f.y;
            z[i] = n.z;
            z[i + 4] = f.z;
        }
        
        var xMin = Mathf.Min(x);
        var xMax = Mathf.Max(x);
        var yMin = Mathf.Min(y);
        var yMax = Mathf.Max(y);
        var zMin = Mathf.Min(z);
        var zMax = Mathf.Max(z);

        bbox[0] = TransformH(invW2l, new Vector3(xMin, yMin, zMin), 1.0f);
        bbox[1] = TransformH(invW2l, new Vector3(xMin, yMin, zMax), 1.0f);
        bbox[2] = TransformH(invW2l, new Vector3(xMin, yMax, zMin), 1.0f);
        bbox[3] = TransformH(invW2l, new Vector3(xMin, yMax, zMax), 1.0f);
        bbox[4] = TransformH(invW2l, new Vector3(xMax, yMin, zMin), 1.0f);
        bbox[5] = TransformH(invW2l, new Vector3(xMax, yMin, zMax), 1.0f);
        bbox[6] = TransformH(invW2l, new Vector3(xMax, yMax, zMin), 1.0f);
        bbox[7] = TransformH(invW2l, new Vector3(xMax, yMax, zMax), 1.0f);
    }

    internal Vector3 TransformH(Matrix4x4 m, Vector3 v, float w) {
        var r = m * new Vector4(v.x, v.y, v.z, w);
        return new Vector3(r.x, r.y, r.z);
    }

    public void Dispose() {
        Object.Destroy(_shadowCamera.gameObject);
    }
}
