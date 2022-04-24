using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdvancedRenderPipeline.Editor;
using AdvancedRenderPipeline.Runtime;
using AdvancedRenderPipeline.Runtime.Cameras;
using RP_Tests;
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

    public GraphicsFormat gbuffer0CubemapFormat = GraphicsFormat.R8G8B8A8_UNorm;
    public GraphicsFormat gbuffer1CubemapFormat = GraphicsFormat.R16G16_UNorm;
    public GraphicsFormat gbuffer2CubemapFormat = GraphicsFormat.R16_SFloat;
    
    public GraphicsFormat gbuffer0Format = GraphicsFormat.R8G8B8A8_UNorm;
    public GraphicsFormat gbuffer1Format = GraphicsFormat.R8G8_UNorm;
    public GraphicsFormat gbuffer2Format = GraphicsFormat.R16_SFloat;
    public GraphicsFormat vbuffer0Format = GraphicsFormat.R16G16_SFloat;
    
    public List<RenderTexture> highResolutionGBuffer0 = new();
    public List<RenderTexture> highResolutionGBuffer1 = new();
    public List<RenderTexture> highResolutionGBuffer2 = new();

    public List<RenderTexture> octahdronGBuffer0 = new();
    public List<RenderTexture> octahdronGBuffer1 = new();
    public List<RenderTexture> octahdronGBuffer2 = new();
    public List<RenderTexture> octahdronVBuffer0 = new();

    public static string ProbeDataDir;

    public static AdvancedRenderPipelineSettings pipelineSettings => AdvancedRenderPipeline.Runtime.AdvancedRenderPipeline.settings;
    public static DiffuseGISettings diffuseGISettings => AdvancedRenderPipeline.Runtime.AdvancedRenderPipeline.settings.diffuseGISettings;
    
    [MenuItem("ARP Probes/CaptureDiffuseProbes")]
    public static void CaptureDiffuseProbes() {
        ProbeDataDir = Application.dataPath + "/Data/Probes/";
        ProbeDataDir = "Assets/Data/Probes/";
        var wizard = DisplayWizard<DiffuseProbeBakerWizard>("Diffuse Probe Baker", "Capture", "Extra");
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
        var probeGBufferSize = (int) diffuseGISettings.probeGBufferSize;
        var probeVBufferSize = (int) diffuseGISettings.probeVBufferSize;
        
        var ocDesc0 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer0Format, 0, 1) {
            dimension = TextureDimension.Tex2D,
            sRGB = false,
            enableRandomWrite = true,
            useMipMap = false
        };

        var ocDesc1 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer1Format, 0, 1) {
            dimension = TextureDimension.Tex2D,
            sRGB = false,
            enableRandomWrite = true,
            useMipMap = false
        };

        var ocDesc2 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer2Format, 0, 1) {
            dimension = TextureDimension.Tex2D,
            sRGB = false,
            enableRandomWrite = true,
            useMipMap = false
        };

        var ocDesc3 = new RenderTextureDescriptor(probeVBufferSize, probeVBufferSize, vbuffer0Format, 0, 1) {
            dimension = TextureDimension.Tex2D,
            sRGB = false,
            enableRandomWrite = true,
            useMipMap = false
        };
        
        var gbuffer0 = new RenderTexture(ocDesc0);
        var gbuffer1 = new RenderTexture(ocDesc1);
        var gbuffer2 = new RenderTexture(ocDesc2);
        var vbuffer0 = new RenderTexture(ocDesc3);
        gbuffer0.Create();
        gbuffer1.Create();
        gbuffer2.Create();
        vbuffer0.Create();
        
        var gbuffer0Path = Path.Combine(ProbeDataDir, "cm_oc_b0.asset");
        var gbuffer1Path = Path.Combine(ProbeDataDir, "cm_oc_b1.asset");
        var gbuffer2Path = Path.Combine(ProbeDataDir, "cm_oc_b2.asset");
        var vbuffer0Path = Path.Combine(ProbeDataDir, "cm_oc_v0.asset");
            
        ARPEditorAsset.CreateOrOverrideAssetAt(gbuffer0, gbuffer0Path);
        ARPEditorAsset.CreateOrOverrideAssetAt(gbuffer1, gbuffer1Path);
        ARPEditorAsset.CreateOrOverrideAssetAt(gbuffer2, gbuffer2Path);
        ARPEditorAsset.CreateOrOverrideAssetAt(vbuffer0, vbuffer0Path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void Clear() {
        foreach (var rt in highResolutionGBuffer0.Where(rt => rt.IsCreated())) rt.Release();
        foreach (var rt in highResolutionGBuffer1.Where(rt => rt.IsCreated())) rt.Release();
        foreach (var rt in highResolutionGBuffer2.Where(rt => rt.IsCreated())) rt.Release();
        foreach (var rt in octahdronGBuffer0.Where(rt => rt.IsCreated())) rt.Release();
        foreach (var rt in octahdronGBuffer1.Where(rt => rt.IsCreated())) rt.Release();
        foreach (var rt in octahdronGBuffer2.Where(rt => rt.IsCreated())) rt.Release();
        foreach (var rt in octahdronVBuffer0.Where(rt => rt.IsCreated())) rt.Release();
        
        highResolutionGBuffer0.Clear();
        highResolutionGBuffer1.Clear();
        highResolutionGBuffer2.Clear();
        octahdronGBuffer0.Clear();
        octahdronGBuffer1.Clear();
        octahdronGBuffer2.Clear();
        octahdronVBuffer0.Clear();
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
        cam.nearClipPlane = 0.0001f;
        cam.farClipPlane = diffuseGISettings.probeViewDistance;
        cam.clearFlags = CameraClearFlags.Skybox;
        
        var additionalData = go.AddComponent<ARPCameraAdditionalData>();
        additionalData.cameraType = AdvancedCameraType.DiffuseProbe;

        var skybox = go.AddComponent<Skybox>();
        skybox.material = MaterialManager.DiffuseProbeSkyboxMat;

        var placeholderRT = new RenderTexture(offlineCubemapSize, offlineCubemapSize, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None);
        placeholderRT.Create();

        cam.targetTexture = placeholderRT;

        try {
            var hrDesc0 = new RenderTextureDescriptor(offlineCubemapSize, offlineCubemapSize, gbuffer0CubemapFormat, 0, 1) {
                dimension = TextureDimension.Cube,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };

            var hrDesc1 = new RenderTextureDescriptor(offlineCubemapSize, offlineCubemapSize, gbuffer1CubemapFormat, 0, 1) {
                dimension = TextureDimension.Cube,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };

            var hrDesc2 = new RenderTextureDescriptor(offlineCubemapSize, offlineCubemapSize, gbuffer2CubemapFormat, 0, 1) {
                dimension = TextureDimension.Cube,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };

            var ocDesc0 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer0Format, 0, 1) {
                dimension = TextureDimension.Tex2D,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };

            var ocDesc1 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer1Format, 0, 1) {
                dimension = TextureDimension.Tex2D,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };

            var ocDesc2 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer2Format, 0, 1) {
                dimension = TextureDimension.Tex2D,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };

            var ocDesc3 = new RenderTextureDescriptor(probeVBufferSize, probeVBufferSize, vbuffer0Format, 0, 1) {
                dimension = TextureDimension.Tex2D,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };
            
            var arrDesc2 = new RenderTextureDescriptor(probeGBufferSize, probeGBufferSize, gbuffer2Format, 0, 1) {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = count,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };

            var arrDesc3 = new RenderTextureDescriptor(probeVBufferSize, probeVBufferSize, vbuffer0Format, 0, 1) {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = count,
                sRGB = false,
                enableRandomWrite = true,
                useMipMap = false
            };
            
            var gbufferCubemap0 = new RenderTexture(hrDesc0);
            var gbufferCubemap1 = new RenderTexture(hrDesc1);
            var gbufferCubemap2 = new RenderTexture(hrDesc2);
            var gbuffer0 = new RenderTexture(ocDesc0);
            var gbuffer1 = new RenderTexture(ocDesc1);
            var gbuffer2 = new RenderTexture(ocDesc2);
            var vbuffer0 = new RenderTexture(ocDesc3);
            gbufferCubemap0.Create();
            gbufferCubemap1.Create();
            gbufferCubemap2.Create();
            gbuffer0.Create();
            gbuffer1.Create();
            gbuffer2.Create();
            vbuffer0.Create();
            additionalData.diffuseProbeGBufferCubemap0 = gbufferCubemap0;
            additionalData.diffuseProbeGBufferCubemap1 = gbufferCubemap1;
            additionalData.diffuseProbeGBufferCubemap2 = gbufferCubemap2;
            additionalData.diffuseProbeGBuffer0 = gbuffer0;
            additionalData.diffuseProbeGBuffer1 = gbuffer1;
            additionalData.diffuseProbeGBuffer2 = gbuffer2;
            additionalData.diffuseProbeVBuffer0 = vbuffer0;
            
            // var gbuffer0Arr = new Texture2D(probeGBufferSize * count, probeGBufferSize, gbuffer0Format, TextureCreationFlags.None);
            // var gbuffer1Arr = new Texture2D(probeGBufferSize * count, probeGBufferSize, gbuffer1Format, TextureCreationFlags.None);
            // var gbuffer2Arr = new Texture2D(probeGBufferSize * count, probeGBufferSize, gbuffer2Format, TextureCreationFlags.None);
            // var vbuffer0Arr = new Texture2D(probeVBufferSize * count, probeVBufferSize, vbuffer0Format, TextureCreationFlags.None);
            var gbuffer0Arr = new Texture2DArray(probeGBufferSize, probeGBufferSize, count, gbuffer0Format, TextureCreationFlags.None);
            var gbuffer1Arr = new Texture2DArray(probeGBufferSize, probeGBufferSize, count, gbuffer1Format, TextureCreationFlags.None);
            var gbuffer2Arr = new Texture2DArray(probeGBufferSize, probeGBufferSize, count, gbuffer2Format, TextureCreationFlags.None);
            var vbuffer0Arr = new Texture2DArray(probeVBufferSize, probeVBufferSize, count, vbuffer0Format, TextureCreationFlags.None);
            
            var gbuffer0Single = new Texture2D(probeGBufferSize, probeGBufferSize, gbuffer0Format, TextureCreationFlags.None);
            var gbuffer1Single = new Texture2D(probeGBufferSize, probeGBufferSize, gbuffer1Format, TextureCreationFlags.None);
            var gbuffer2Single = new Texture2D(probeGBufferSize, probeGBufferSize, gbuffer2Format, TextureCreationFlags.None);
            var vbuffer0Single = new Texture2D(probeVBufferSize, probeVBufferSize, vbuffer0Format, TextureCreationFlags.None);

            for (var i = 0; i < dimensions.x; i++) {
                for (var j = 0; j < dimensions.y; j++) {
                    for (var k = 0; k < dimensions.z; k++) {
                        var probeId = diffuseGISettings.GetProbeIndex1d(new Vector3Int(i, j, k));
                        var pos = origin + new Vector3(i * maxIntervals.x, j * maxIntervals.y, k * maxIntervals.z);
                        tr.position = pos;

                        /*
                        highResolutionGBuffer0.Add(gbufferCubemap0);
                        highResolutionGBuffer1.Add(gbufferCubemap1);
                        highResolutionGBuffer2.Add(gbufferCubemap2);
                        octahdronGBuffer0.Add(gbuffer0);
                        octahdronGBuffer1.Add(gbuffer1);
                        octahdronGBuffer2.Add(gbuffer2);
                        octahdronVBuffer0.Add(vbuffer0);
                        */
                        
                        cam.Render();

                        var temp = RenderTexture.active;

                        RenderTexture.active = gbuffer0;
                        gbuffer0Single.ReadPixels(new Rect(0, 0, probeGBufferSize, probeGBufferSize), 0, 0, false);
                        RenderTexture.active = gbuffer1;
                        gbuffer1Single.ReadPixels(new Rect(0, 0, probeGBufferSize, probeGBufferSize), 0, 0, false);
                        RenderTexture.active = gbuffer2;
                        gbuffer2Single.ReadPixels(new Rect(0, 0, probeGBufferSize, probeGBufferSize), 0, 0, false);
                        RenderTexture.active = vbuffer0;
                        vbuffer0Single.ReadPixels(new Rect(0, 0, probeVBufferSize, probeVBufferSize), 0, 0, false);
                        RenderTexture.active = temp;
                        
                        gbuffer0Arr.SetPixels(gbuffer0Single.GetPixels(), probeId, 0);
                        gbuffer1Arr.SetPixels(gbuffer1Single.GetPixels(), probeId, 0);
                        gbuffer2Arr.SetPixels(gbuffer2Single.GetPixels(), probeId, 0);
                        vbuffer0Arr.SetPixels(vbuffer0Single.GetPixels(), probeId, 0);
                    }
                }
            }
            
            gbufferCubemap0.Release();
            gbufferCubemap1.Release();
            gbufferCubemap2.Release();
            gbuffer0.Release();
            gbuffer1.Release();
            gbuffer2.Release();
            vbuffer0.Release();

            /*
            var gbuffer0Data = gbuffer0Arr.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Application.dataPath + "/Data/Probes/", $"{diffuseGISettings.probeGBufferPath0}.png"), gbuffer0Data);
            var gbuffer1Data = gbuffer1Arr.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Application.dataPath + "/Data/Probes/", $"{diffuseGISettings.probeGBufferPath1}.png"), gbuffer1Data);
            var gbuffer2Data = gbuffer2Arr.EncodeToEXR();
            File.WriteAllBytes(Path.Combine(Application.dataPath + "/Data/Probes/", $"{diffuseGISettings.probeGBufferPath2}.exr"), gbuffer2Data);
            var vbuffer0Data = vbuffer0Arr.EncodeToEXR();
            File.WriteAllBytes(Path.Combine(Application.dataPath + "/Data/Probes/", $"{diffuseGISettings.probeVBufferPath0}.exr"), vbuffer0Data);
            */

            var gbuffer0Path = Path.Combine(ProbeDataDir, $"{diffuseGISettings.probeGBufferPath0}.asset");
            var gbuffer1Path = Path.Combine(ProbeDataDir, $"{diffuseGISettings.probeGBufferPath1}.asset");
            var gbuffer2Path = Path.Combine(ProbeDataDir, $"{diffuseGISettings.probeGBufferPath2}.asset");
            var vbuffer0Path = Path.Combine(ProbeDataDir, $"{diffuseGISettings.probeVBufferPath0}.asset");
            
            ARPEditorAsset.CreateOrOverrideAssetAt(gbuffer0Arr, gbuffer0Path);
            ARPEditorAsset.CreateOrOverrideAssetAt(gbuffer1Arr, gbuffer1Path);
            ARPEditorAsset.CreateOrOverrideAssetAt(gbuffer2Arr, gbuffer2Path);
            ARPEditorAsset.CreateOrOverrideAssetAt(vbuffer0Arr, vbuffer0Path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var giSettings = diffuseGISettings;
            giSettings.probeGBufferArr0 = AssetDatabase.LoadAssetAtPath<Texture2DArray>(gbuffer0Path);
            giSettings.probeGBufferArr1 = AssetDatabase.LoadAssetAtPath<Texture2DArray>(gbuffer1Path);
            giSettings.probeGBufferArr2 = AssetDatabase.LoadAssetAtPath<Texture2DArray>(gbuffer2Path);
            giSettings.probeVBufferArr0 = AssetDatabase.LoadAssetAtPath<Texture2DArray>(vbuffer0Path);
            /*
            giSettings.probeGBufferArr0 = gbuffer0Arr;
            giSettings.probeGBufferArr1 = gbuffer1Arr;
            giSettings.probeGBufferArr2 = gbuffer2Arr;
            giSettings.probeVBufferArr0 = vbuffer0Arr;
            */
            giSettings.markProbesDirty = true;
            pipelineSettings.diffuseGISettings = giSettings;
        } finally {
            DestroyImmediate(go);
            Clear();
            placeholderRT.Release();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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