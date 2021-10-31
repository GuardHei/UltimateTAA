using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class LightManager {
	public static bool MainLightIsAvailable => mainLight != null;
	public static bool MainLightShadowAvailable => MainLightIsAvailable && mainLight.shadows != LightShadows.None;
		
	public static Light mainLight;
}

[Serializable]
public class MainLightParams {
	public bool enabled = true;
	public bool shadowOn = true;
	public int shadowDistance = 100;
	public int shadowResolution = 2048;
	public int shadowCascades = 4;
	public Vector3 shadowCascadeSplits = new Vector3(.067f, .2f, .467f);
}

[Serializable]
public struct DirectionalLight {
	public float4 direction;
	public float4 color; // rgb - final light color, a - unused
	public float4 shadow; // rgb - shadow color, a - shadow strength
	public int shadowIndex;
}
