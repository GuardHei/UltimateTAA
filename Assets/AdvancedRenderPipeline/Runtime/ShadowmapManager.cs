using System;
using System.Collections.Generic;
using AdvancedRenderPipeline.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public class ShadowmapManager : IDisposable {

    public static ShadowSettings shadowSettings => AdvancedRenderPipeline.Runtime.AdvancedRenderPipeline.settings.shadowSettings;

    public Camera ShadowCamera => _shadowCamera;

    internal Camera _shadowCamera;
    internal Transform _shadowCameraTransform;

    #region Directional Light Shadow Params

    public readonly float[] dlOrthoWidths = new float[4];

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
        _shadowCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _shadowCamera.enabled = false;
        _shadowCameraTransform = _shadowCamera.transform;
    }

    internal void ComputeLightSpaceAABB(Vector3 lightDir, Vector3[] near, Vector3[] far, ref Vector3[] bbox) {
        var invW2l = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        var w2l = invW2l.inverse;
        
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

    public void UpdateCSM(Camera cam, Vector3 lightDir) {
        var viewport = new Rect(0f, 0f, 1f, 1f);
        cam.CalculateFrustumCorners(viewport, cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, _dlNearCorners);
        cam.CalculateFrustumCorners(viewport, shadowSettings.GetShadowDistance(cam), Camera.MonoOrStereoscopicEye.Mono, _dlFarCorners);

        var camTrans = cam.transform;
        var camPos = camTrans.position;

        var ratios = shadowSettings.MainLightShadowCascadeRatios;

        for (var i = 0; i < 4; i++) {
            _dlNearCorners[i] = camTrans.TransformVector(_dlNearCorners[i]) + camPos;
            _dlFarCorners[i] = camTrans.TransformVector(_dlFarCorners[i]) + camPos;

            var dir = _dlFarCorners[i] - _dlNearCorners[i];
            
            _dlNearCorners0[i] = _dlNearCorners[i];
            _dlFarCorners0[i] = _dlNearCorners0[i] + dir * ratios[0];
            
            _dlNearCorners1[i] = _dlFarCorners0[i];
            _dlFarCorners1[i] = _dlNearCorners1[i] + dir * ratios[1];
            
            _dlNearCorners2[i] = _dlFarCorners1[i];
            _dlFarCorners2[i] = _dlNearCorners2[i] + dir * ratios[2];
            
            _dlNearCorners3[i] = _dlFarCorners2[i];
            _dlFarCorners3[i] = _dlNearCorners3[i] + dir * ratios[3];
        }

        /*
        for (var i = 0; i < 4; i++) {
            var dir = _dlFarCorners[i] - _dlNearCorners[i];
            
            _dlNearCorners0[i] = _dlNearCorners[i];
            _dlFarCorners0[i] = _dlNearCorners0[i] + dir * ratios[0];
            
            _dlNearCorners1[i] = _dlFarCorners0[i];
            _dlFarCorners1[i] = _dlNearCorners1[i] + dir * ratios[1];
            
            _dlNearCorners2[i] = _dlFarCorners1[i];
            _dlFarCorners2[i] = _dlNearCorners2[i] + dir * ratios[2];
            
            _dlNearCorners3[i] = _dlFarCorners2[i];
            _dlFarCorners3[i] = _dlNearCorners3[i] + dir * ratios[3];
        }
        */
        
        ComputeLightSpaceAABB(lightDir, _dlNearCorners0, _dlFarCorners0, ref _dlBBox0);
        ComputeLightSpaceAABB(lightDir, _dlNearCorners1, _dlFarCorners1, ref _dlBBox1);
        ComputeLightSpaceAABB(lightDir, _dlNearCorners2, _dlFarCorners2, ref _dlBBox2);
        ComputeLightSpaceAABB(lightDir, _dlNearCorners3, _dlFarCorners3, ref _dlBBox3);

        dlOrthoWidths[0] = (_dlFarCorners0[2] - _dlNearCorners0[0]).magnitude;
        dlOrthoWidths[1] = (_dlFarCorners1[2] - _dlNearCorners1[0]).magnitude;
        dlOrthoWidths[2] = (_dlFarCorners2[2] - _dlNearCorners2[0]).magnitude;
        dlOrthoWidths[3] = (_dlFarCorners3[2] - _dlNearCorners3[0]).magnitude;
    }

    public void SetupCSM(Vector3 lightDir, int cascade, float nearOffset, float farOffset) {
        Vector3[] bbox;
        Vector3[] n;
        Vector3[] f;

        switch (cascade) {
            case 0: {
                bbox = _dlBBox0;
                n = _dlNearCorners0;
                f = _dlFarCorners0;
                break;
            }
            case 1: {
                bbox = _dlBBox1;
                n = _dlNearCorners1;
                f = _dlFarCorners1;
                break;
            }
            case 2: {
                bbox = _dlBBox2;
                n = _dlNearCorners2;
                f = _dlFarCorners2;
                break;
            }
            case 3: {
                bbox = _dlBBox3;
                n = _dlNearCorners3;
                f = _dlFarCorners3;
                break;
            }

            default: {
                Debug.Log("Assigned cascade is out of bound!");
                return;
            }
        }

        var resolution = (float) shadowSettings.mainLightShadowmapSize;
        var center = (bbox[3] + bbox[4]) * .5f;
        var w = (bbox[0] - bbox[4]).magnitude;
        var h = (bbox[0] - bbox[2]).magnitude;
        var len = (f[2] - n[0]).magnitude;
        var texelDist = len / resolution;
        
        var invW2l = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        var w2l = invW2l.inverse;

        center = TransformH(w2l, center, 1.0f);
        center.x = Mathf.Floor(center.x / texelDist) * texelDist;
        center.y = Mathf.Floor(center.y / texelDist) * texelDist;
        center.z = Mathf.Floor(center.z / texelDist) * texelDist;
        center = TransformH(invW2l, center, 1.0f);
        
        _shadowCameraTransform.rotation = Quaternion.LookRotation(lightDir);
        _shadowCameraTransform.position = center;
        
        _shadowCamera.orthographic = true;
        // _shadowCamera.aspect = w / h;
        _shadowCamera.aspect = 1.0f;
        _shadowCamera.orthographicSize = len * .5f;
        _shadowCamera.nearClipPlane = -nearOffset;
        _shadowCamera.farClipPlane = farOffset;
    }
    
    internal void DrawFrustum(Vector3[] nearCorners, Vector3[] farCorners, Color color) {
        for (var i = 0; i < 4; i++) Debug.DrawLine(nearCorners[i], farCorners[i], color);

        Debug.DrawLine(farCorners[0], farCorners[1], color);
        Debug.DrawLine(farCorners[0], farCorners[3], color);
        Debug.DrawLine(farCorners[2], farCorners[1], color);
        Debug.DrawLine(farCorners[2], farCorners[3], color);
        Debug.DrawLine(nearCorners[0], nearCorners[1], color);
        Debug.DrawLine(nearCorners[0], nearCorners[3], color);
        Debug.DrawLine(nearCorners[2], nearCorners[1], color);
        Debug.DrawLine(nearCorners[2], nearCorners[3], color);
    }


    internal void DrawAABB(Vector3[] points, Color color) {
        Debug.DrawLine(points[0], points[1], color);
        Debug.DrawLine(points[0], points[2], color);
        Debug.DrawLine(points[0], points[4], color);
        
        Debug.DrawLine(points[6], points[2], color);
        Debug.DrawLine(points[6], points[7], color);
        Debug.DrawLine(points[6], points[4], color);

        Debug.DrawLine(points[5], points[1], color);
        Debug.DrawLine(points[5], points[7], color);
        Debug.DrawLine(points[5], points[4], color);

        Debug.DrawLine(points[3], points[1], color);
        Debug.DrawLine(points[3], points[2], color);
        Debug.DrawLine(points[3], points[7], color);
    }

    public void DrawBBoxes() => DrawBBoxes(Color.yellow, Color.magenta, Color.green, Color.cyan);
    
    public void DrawBBoxes(Color c0, Color c1, Color c2, Color c3) {
        DrawAABB(_dlBBox0, c0);  
        DrawAABB(_dlBBox1, c1);
        DrawAABB(_dlBBox2, c2);
        DrawAABB(_dlBBox3, c3);
    }

    public void DrawFrustums() => DrawFrustums(Color.yellow, Color.magenta, Color.green, Color.cyan);
    
    public void DrawFrustums(Color c0, Color c1, Color c2, Color c3) {
        DrawFrustum(_dlNearCorners0, _dlFarCorners0, c0);
        DrawFrustum(_dlNearCorners1, _dlFarCorners1, c1);
        DrawFrustum(_dlNearCorners2, _dlFarCorners2, c2);
        DrawFrustum(_dlNearCorners3, _dlFarCorners3, c3);
    }
    
    public void DrawEntireFrustum() => DrawEntireFrustum(Color.white);

    public void DrawEntireFrustum(Color color) => DrawFrustum(_dlNearCorners, _dlFarCorners, color);

    public void Dispose() => Dispose(true);

    public void Dispose(bool immediate) {
        if (_shadowCamera) {
            if (immediate) Object.DestroyImmediate(_shadowCamera.gameObject);
            else Object.Destroy(_shadowCamera.gameObject);
            // Object.DestroyImmediate(_shadowCamera.gameObject);
        }
    }
}
