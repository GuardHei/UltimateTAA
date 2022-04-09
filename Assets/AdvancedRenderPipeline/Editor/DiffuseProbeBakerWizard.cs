using System.Collections;
using System.Collections.Generic;
using System.IO;
using AdvancedRenderPipeline.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class DiffuseProbeBakerWizard : ScriptableWizard {

    public int cubemapResolution = 256;
    public int octhedronResolution = 16;
    public int depthOcthedronResolution = 16;

    public Vector3 offset;
    public float viewDistance;

    public Transform renderFromPosition;
    public string cubemapPath;
    public Cubemap targetCubemap;
    
    [MenuItem("ARP Probes/CaptureCubemap")]
    public static void CaptureCubemap() {
        var wizard = DisplayWizard<DiffuseProbeBakerWizard>("Diffuse Probe Baker", "Capture", "Clear");
        var transforms = Selection.transforms;
        if (transforms is { Length: > 0 }) wizard.renderFromPosition = transforms[0];
    }

    public void OnWizardUpdate() {
        helpString = "Select transform to render from";
        isValid = renderFromPosition != null;
        isValid &= (!string.IsNullOrWhiteSpace(cubemapPath) || targetCubemap);
    }

    public void OnWizardCreate() {

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
            
            MaterialManager.IndirectSpecularMat.EnableKeyword(ShaderKeywordManager.ACCURATE_TRANSFORM_ON);
        
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
                File.WriteAllBytes(Path.Combine(Application.dataPath + "/Data/Probes/", $"{cubemapPath}.png"), textureData);
            }
            
            // save to disk
            AssetDatabase.SaveAssetIfDirty(cubemap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } finally {
            MaterialManager.IndirectSpecularMat.DisableKeyword(ShaderKeywordManager.ACCURATE_TRANSFORM_ON);
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