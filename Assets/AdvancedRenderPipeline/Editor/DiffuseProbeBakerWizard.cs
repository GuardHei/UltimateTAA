using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AdvancedRenderPipeline.Runtime;
using AdvancedRenderPipeline.Runtime.Cameras;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class DiffuseProbeBakerWizard : ScriptableWizard {

    public int cubemapResolution = 256;
    public float viewDistance;

    public bool dontCreateCamera = true;
    public Transform renderFromPosition;
    public string cubemapPath;
    public Cubemap targetCubemap;

    public GraphicsFormat gbuffer0Format = GraphicsFormat.R8G8B8A8_UNorm;
    public GraphicsFormat gbuffer1Format = GraphicsFormat.R8G8_UNorm;
    public GraphicsFormat gbuffer2Format = GraphicsFormat.R16G16_SFloat;
    
    public List<RenderTexture> highResolutionGBuffer0 = new();
    public List<RenderTexture> highResolutionGBuffer1 = new();
    public List<RenderTexture> highResolutionGBuffer2 = new();
    
    public List<RenderTexture> lowResolutionGBuffer0 = new();
    public List<RenderTexture> lowResolutionGBuffer1 = new();
    public List<RenderTexture> lowResolutionGBuffer2 = new();
    
    public List<RenderTexture> octahdronGBuffer0 = new();
    public List<RenderTexture> octahdronGBuffer1 = new();
    public List<RenderTexture> octahdronGBuffer2 = new();

    public static string ProbeDataDir;
    public static DiffuseGISettings diffuseGISettings => AdvancedRenderPipeline.Runtime.AdvancedRenderPipeline.settings.diffuseGISettings;
    
    [MenuItem("ARP Probes/CaptureCubemap")]
    public static void CaptureCubemap() {
        ProbeDataDir = Application.dataPath + "/Data/Probes/";
        var wizard = DisplayWizard<DiffuseProbeBakerWizard>("Diffuse Probe Baker", "Capture", "Clear");
        var transforms = Selection.transforms;
        if (transforms is { Length: > 0 }) wizard.renderFromPosition = transforms[0];
        wizard.OnWizardUpdate();
    }

    public void OnWizardUpdate() {
        isValid = true;
        // helpString = "Select transform to render from";
        // isValid = renderFromPosition != null;
        // isValid &= (!string.IsNullOrWhiteSpace(cubemapPath) || targetCubemap);
    }

    public void OnWizardCreate() {
        // CaptureGBuffer();
        CaptureProbeGBuffer();
    }

    public void OnWizardOtherButton() {
        Clear();
    }

    public void Clear() {
        foreach (var rt in highResolutionGBuffer0) {
            if (rt.IsCreated()) rt.Release();
        }
        
        highResolutionGBuffer0.Clear();
        
        foreach (var rt in highResolutionGBuffer1) {
            if (rt.IsCreated()) rt.Release();
        }
        
        highResolutionGBuffer1.Clear();
        
        foreach (var rt in highResolutionGBuffer2) {
            if (rt.IsCreated()) rt.Release();
        }
        
        highResolutionGBuffer2.Clear();
    }

    public void CaptureProbeGBuffer() {
        var count = diffuseGISettings.Count;
        var dimensions = diffuseGISettings.dimensions;
        var maxIntervals = diffuseGISettings.maxIntervals;
        var origin = diffuseGISettings.Min;

        var offlineCubemapSize = (int) diffuseGISettings.offlineCubemapSize;
        var probeGBufferSize = (int) diffuseGISettings.probeGBufferSize;
        var probeVBufferSize = (int) diffuseGISettings.probeVBufferSize;

        var go = new GameObject("Diffuse Probe Capturer");
        var tr = go.transform;
        var cam = go.AddComponent<Camera>();
        cam.farClipPlane = diffuseGISettings.probeViewDistance;
        var additionalData = go.AddComponent<ARPCameraAdditionalData>();
        additionalData.cameraType = AdvancedCameraType.DiffuseProbe;

        var placeholderRT = new RenderTexture(offlineCubemapSize, offlineCubemapSize, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None);
        placeholderRT.Create();

        cam.targetTexture = placeholderRT;

        try {
            var hrDesc0 = new RenderTextureDescriptor(offlineCubemapSize, offlineCubemapSize, gbuffer0Format, 0, 1) {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = false
            };

            var hrDesc1 = new RenderTextureDescriptor(offlineCubemapSize, offlineCubemapSize, gbuffer1Format, 0, 1) {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = false
            };

            var hrDesc2 = new RenderTextureDescriptor(offlineCubemapSize, offlineCubemapSize, gbuffer2Format, 0, 1) {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = false
            };
            
            var lrDesc0 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer0Format, 0, 1) {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = false
            };

            var lrDesc1 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer1Format, 0, 1) {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = false
            };

            var lrDesc2 = new RenderTextureDescriptor(probeVBufferSize, probeVBufferSize, gbuffer2Format, 0, 1) {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = false
            };
            
            var ocDesc0 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer0Format, 0, 1) {
                dimension = TextureDimension.Tex2DArray,
                enableRandomWrite = true,
                useMipMap = false
            };

            var ocDesc1 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer1Format, 0, 1) {
                dimension = TextureDimension.Tex2DArray,
                enableRandomWrite = true,
                useMipMap = false
            };

            var ocDesc2 = new RenderTextureDescriptor(probeVBufferSize, probeVBufferSize, gbuffer2Format, 0, 1) {
                dimension = TextureDimension.Tex2DArray,
                enableRandomWrite = true,
                useMipMap = false
            };

            for (var i = 0; i < dimensions.x; i++) {
                for (var j = 0; j < dimensions.y; j++) {
                    for (var k = 0; k < dimensions.z; k++) {
                        var pos = origin + new Vector3(i * maxIntervals.x, j * maxIntervals.y, k * maxIntervals.z);
                        tr.position = pos;
                        var gbuffer0 = new RenderTexture(hrDesc0);
                        var gbuffer1 = new RenderTexture(hrDesc1);
                        var gbuffer2 = new RenderTexture(hrDesc2);
                        gbuffer0.Create();
                        gbuffer1.Create();
                        gbuffer2.Create();
                        additionalData.diffuseProbeGBuffer0 = gbuffer0;
                        additionalData.diffuseProbeGBuffer1 = gbuffer1;
                        additionalData.diffuseProbeGBuffer2 = gbuffer2;
                        highResolutionGBuffer0.Add(gbuffer0);
                        highResolutionGBuffer1.Add(gbuffer1);
                        highResolutionGBuffer2.Add(gbuffer2);
                        cam.Render();
                    }
                }
            }
        } finally {
            DestroyImmediate(go);
            placeholderRT.Release();
        }
        
        
    }

    public void CaptureGBuffer() {
        // if (string.IsNullOrWhiteSpace(cubemapPath) && !targetCubemap) return;
        if (dontCreateCamera) {
            var go = renderFromPosition.gameObject;
            var cam = go.GetComponent<Camera>();
            if (!cam) return;
            cam.farClipPlane = viewDistance;
            cam.Render();
        }
    }

    public void Capture() {
        if (string.IsNullOrWhiteSpace(cubemapPath) && !targetCubemap) return;
        
        // create temporary camera for rendering
        GameObject go = new GameObject("CubemapCamera");
        var cam = go.AddComponent<Camera>();

        try {
            // place it on the object
            go.transform.position = renderFromPosition.position;
            go.transform.rotation = Quaternion.identity;

            var useTex2d = !targetCubemap;

            Cubemap cubemap = !useTex2d ? targetCubemap : new Cubemap(cubemapResolution, TextureFormat.RGB24, false);
            
            // MaterialManager.IndirectSpecularMat.EnableKeyword(ShaderKeywordManager.ACCURATE_TRANSFORM_ON);
        
            // render into cubemap
            cam.RenderToCubemap(cubemap);

            if (useTex2d) {
                // convert cubemap to single horizontal texture
                var texture = new Texture2D(cubemapResolution * 6, cubemapResolution, cubemap.format, false);
                int texturePixelCount = (cubemapResolution * 6) * cubemapResolution;
                var texturePixels = new Color[texturePixelCount];
 
                var cubeFacePixels = cubemap.GetPixels(CubemapFace.PositiveX);
                CopyTextureIntoCubemapRegion(cubeFacePixels, texturePixels, cubemapResolution * 0, cubemapResolution);
                cubeFacePixels = cubemap.GetPixels(CubemapFace.NegativeX);
                CopyTextureIntoCubemapRegion(cubeFacePixels, texturePixels, cubemapResolution * 1, cubemapResolution);
 
                cubeFacePixels = cubemap.GetPixels(CubemapFace.PositiveY);
                CopyTextureIntoCubemapRegion(cubeFacePixels, texturePixels, cubemapResolution * 2, cubemapResolution);
                cubeFacePixels = cubemap.GetPixels(CubemapFace.NegativeY);
                CopyTextureIntoCubemapRegion(cubeFacePixels, texturePixels, cubemapResolution * 3, cubemapResolution);
 
                cubeFacePixels = cubemap.GetPixels(CubemapFace.PositiveZ);
                CopyTextureIntoCubemapRegion(cubeFacePixels, texturePixels, cubemapResolution * 4, cubemapResolution);
                cubeFacePixels = cubemap.GetPixels(CubemapFace.NegativeZ);
                CopyTextureIntoCubemapRegion(cubeFacePixels, texturePixels, cubemapResolution * 5, cubemapResolution);
 
                texture.SetPixels(texturePixels, 0);
 
                // write texture as png to disk
                var textureData = texture.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(ProbeDataDir, $"{cubemapPath}.png"), textureData);
            }
            
            // save to disk
            AssetDatabase.SaveAssetIfDirty(cubemap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } finally {
            // MaterialManager.IndirectSpecularMat.DisableKeyword(ShaderKeywordManager.ACCURATE_TRANSFORM_ON);
            // destroy temporary camera
            DestroyImmediate(go);
        }
    }
    
    public static void CopyTextureIntoCubemapRegion(Color[] srcPixels, Color[] dstPixels, int xOffsetDst, int cubemapSize) {
        int cubemapWidth = cubemapSize * 6;
        for (int y = 0; y != cubemapSize; ++y) {
            int j = cubemapSize - 1 - y;
            for (int x = 0; x != cubemapSize; ++x) {
                int i = cubemapSize - 1 - x;
                int iSrc = x + (j * cubemapSize);
                int iDst = (x + xOffsetDst) + (y * cubemapWidth);
                dstPixels[iDst] = srcPixels[iSrc];
            }
        }
    }
}