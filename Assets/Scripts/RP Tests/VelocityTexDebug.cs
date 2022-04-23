using System;
using System.Collections.Generic;
using AdvancedRenderPipeline.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

public class VelocityTexDebug : MonoBehaviour {

    public Transform target;

    public bool move;

    public Transform from;
    public Transform to;
    public float interval;

    public Material mat;
    public MeshRenderer meshRenderer;

    private void Update() {
        if (!target || !from || !to) return;

        if (move) {
            float c = Mathf.PingPong(Time.time, interval) / interval;
            target.position = Vector3.Lerp(from.position, to.position, c);
        }

        if (mat) {
            // Debug.Log(mat.name + ": " + mat.GetShaderPassEnabled(ShaderTagManager.MOTION_VECTORS_PASS));
            if (mat.GetShaderPassEnabled(ShaderTagManager.MOTION_VECTORS_PASS)) Debug.Log("Issue found!");
        }
        
        if (meshRenderer) print(meshRenderer.localToWorldMatrix.ToString("F3"));
    }
}
