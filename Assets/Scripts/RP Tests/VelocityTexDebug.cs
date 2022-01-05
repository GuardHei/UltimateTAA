using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VelocityTexDebug : MonoBehaviour {

    public Transform target;

    public Transform from;
    public Transform to;
    public float interval;

    private void Update() {
        if (!target || !from || !to) return;
        float c = Mathf.PingPong(Time.time, interval) / interval;
        target.position = Vector3.Lerp(from.position, to.position, c);
    }
}
