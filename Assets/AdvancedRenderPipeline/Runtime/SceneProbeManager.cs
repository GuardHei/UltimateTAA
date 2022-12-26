using System;
using System.IO;
using AdvancedRenderPipeline.Runtime.CustomAttributes;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace AdvancedRenderPipeline.Runtime {
    public class SceneProbeManager : MonoBehaviour {

        public const string PROBE_DATA_DIR = "Assets/Data/Probes/";
        public const string PROBE_GBUFFER0_PATH = "_diff_probe_gbuff0.asset";
        public const string PROBE_GBUFFER1_PATH = "_diff_probe_gbuff1.asset";
        public const string PROBE_GBUFFER2_PATH = "_diff_probe_gbuff2.asset";
        public const string PROBE_VBUFFER_PATH = "_diff_probe_vbuff.asset";
        public const string PROBE_IRRADIANCE_PATH = "_diff_probe_irr.asset";
        
        public bool markProbesDirty;
        [DisplayOnly]
        public bool probeDataValidInMem;
        [DisplayOnly]
        public bool probeDataValidInVram;
        public int priority;
        public bool supportsRelit = true;
        public Vector3 volumeCenterOffset;
        public Vector3Int dimensions;
        public Vector3 cellIntervals;
        [Range(.1f, 250f)]
        public float probeViewDistance = 250f;
        [Range(0f, 200f)]
        public float probeDownsampleSharpness = 70f;
        [Range(0f, 5f)]
        public float probeIrradianceMultiplier = 1f;
        [Range(0f, .1f)]
        public float visibilityTestBias = .005f;
        public bool enableVisibilityTest;
        public bool enableMultiBounce;
        public bool enableIndirectShadowSampling;
        public DiffuseGIProbeSize offlineCubemapSize = DiffuseGIProbeSize._512;
        public DiffuseGIProbeSize probeGBufferSize = DiffuseGIProbeSize._16;
        public DiffuseGIProbeSize probeVBufferSize = DiffuseGIProbeSize._24;
        public DiffuseGIProbeSize probeIrradianceSize = DiffuseGIProbeSize._8;

        [DisplayOnly]
        public string probeGBuffer0Name;
        [DisplayOnly]
        public string probeGBuffer1Name;
        [DisplayOnly]
        public string probeGBuffer2Name;
        [DisplayOnly]
        public string probeVBufferName;
        [DisplayOnly]
        public string probeIrradianceName;

        [DisplayOnly]
        public readonly GraphicsFormat offlineGBuffer0CubemapFormat = GraphicsFormat.R8G8B8_UNorm;
        [DisplayOnly]
        public readonly GraphicsFormat offlineGBuffer1CubemapFormat = GraphicsFormat.R16G16_UNorm;
        [DisplayOnly]
        public readonly GraphicsFormat offlineGBuffer2CubemapFormat = GraphicsFormat.R16_SFloat;
        [DisplayOnly]
        public readonly GraphicsFormat offlineVBufferCubemapFormat = GraphicsFormat.R16G16_SFloat;
        [DisplayOnly]
        public readonly GraphicsFormat offlineIrradianceCubemapFormat = GraphicsFormat.R16G16B16A16_SFloat;
        
        [DisplayOnly]
        public readonly GraphicsFormat probeGBuffer0Format = GraphicsFormat.R8G8B8_UNorm;
        [DisplayOnly]
        public readonly GraphicsFormat probeGBuffer1Format = GraphicsFormat.R8G8_UNorm;
        [DisplayOnly]
        public readonly GraphicsFormat probeGBuffer2Format = GraphicsFormat.R16_SFloat;
        [DisplayOnly]
        public readonly GraphicsFormat probeVBufferFormat = GraphicsFormat.R16G16_SFloat;
        [DisplayOnly]
        public readonly GraphicsFormat probeIrradianceFormat = GraphicsFormat.B10G11R11_UFloatPack32;

        public Texture2DArray probeGBuffer0Arr;
        public Texture2DArray probeGBuffer1Arr;
        public Texture2DArray probeGBuffer2Arr;
        public Texture2DArray probeVBufferArr;
        public Texture2DArray probeIrradianceArr;

        private bool prevSupportsRelit;
        private int prevNumProbes = -1;
        private DiffuseGIProbeSize prevProbeGBufferSize = DiffuseGIProbeSize._16;
        private DiffuseGIProbeSize prevProbeVBufferSize = DiffuseGIProbeSize._24;
        private DiffuseGIProbeSize prevProbeIrradianceSize = DiffuseGIProbeSize._8;
        private GraphicsFormat prevProbeGBuffer0Format = GraphicsFormat.R8G8B8_UNorm;
        private GraphicsFormat prevProbeGBuffer1Format = GraphicsFormat.R8G8_UNorm;
        private GraphicsFormat prevProbeGBuffer2Format = GraphicsFormat.R16_SFloat;
        private GraphicsFormat prevProbeVBufferFormat = GraphicsFormat.R16G16_SFloat;
        private GraphicsFormat prevProbeIrradianceFormat = GraphicsFormat.B10G11R11_UFloatPack32;

        public int NumProbes => dimensions.x * dimensions.y * dimensions.z;
		
        public Vector3 Sizes => new((dimensions.x - 1f) * cellIntervals.x, (dimensions.y - 1f) * cellIntervals.y, (dimensions.z - 1f) * cellIntervals.z);

        public Vector3 Min => volumeCenterOffset - Sizes * .5f;
		
        public Vector3 Max => volumeCenterOffset + Sizes * .5f;

        public float VolumeMaxInternalDistance => Vector3.Distance(Max, Min);

        public float CellMaxInternalDistance => Mathf.Sqrt(cellIntervals.x * cellIntervals.x + cellIntervals.y * cellIntervals.y + cellIntervals.z * cellIntervals.z);

        public int GetProbeIndex1d(Vector3Int probe) => (probe.z * dimensions.x * dimensions.y) + (probe.y * dimensions.x) + probe.x;

        public string GetProbeGBuffer0FullPath => PROBE_DATA_DIR + probeGBuffer0Name;
        public string GetProbeGBuffer1FullPath => PROBE_DATA_DIR + probeGBuffer1Name;
        public string GetProbeGBuffer2FullPath => PROBE_DATA_DIR + probeGBuffer2Name;
        public string GetProbeVBufferFullPath => PROBE_DATA_DIR + probeVBufferName;
        public string GetProbeIrradianceFullPath => PROBE_DATA_DIR + probeIrradianceName;
        
        public Vector3Int GetProbeIndex3d(int index) {
            var z = index / (dimensions.x * dimensions.y);
            index -= (z * dimensions.x * dimensions.y);
            var y = index / dimensions.x;
            var x = index % dimensions.x;
            return new Vector3Int(x, y, z);
        }

        private void OnEnable() {
            UpdateProbeDataNames();
            ValidateProbeDataInMem();
        }

        private void OnValidate() {
            UpdateProbeDataNames();
            ValidateProbeDataInMem();
        }

        private void UpdateProbeDataNames() {
            var scene = gameObject.scene;
            var sceneName = scene.name.ToLower();
            
            // scene.name is not guaranteed to be unique as different .scene files of a same name can be stored under different directories.
            // However, we are assuming the uniqueness for the sake of implementation simplicity.
            probeGBuffer0Name = sceneName + PROBE_GBUFFER0_PATH;
            probeGBuffer1Name = sceneName + PROBE_GBUFFER1_PATH;
            probeGBuffer2Name = sceneName + PROBE_GBUFFER2_PATH;
            probeVBufferName = sceneName + PROBE_VBUFFER_PATH;
            probeIrradianceName = sceneName + PROBE_IRRADIANCE_PATH;
        }

        // Check if the probe data exists in the memory and corresponds to the specs specified in the script.
        private bool ValidateProbeDataInMem() {
            probeDataValidInMem = false;

            if (supportsRelit) {
                probeDataValidInMem = probeGBuffer0Arr && probeGBuffer1Arr && probeGBuffer2Arr && probeVBufferArr;
                if (!probeDataValidInMem) return false;

                probeDataValidInMem = probeGBuffer0Arr.dimension == TextureDimension.Tex2DArray 
                    && probeGBuffer1Arr.dimension == TextureDimension.Tex2DArray 
                    && probeGBuffer2Arr.dimension == TextureDimension.Tex2DArray 
                    && probeVBufferArr.dimension == TextureDimension.Tex2DArray;
                if (!probeDataValidInMem) return false;

                var numProbes = NumProbes;
                probeDataValidInMem = probeGBuffer0Arr.depth == numProbes
                    && probeGBuffer1Arr.depth == numProbes
                    && probeGBuffer2Arr.depth == numProbes
                    && probeVBufferArr.depth == numProbes;
                if (!probeDataValidInMem) return false;

                probeDataValidInMem = probeGBuffer0Arr.width == (int) probeGBufferSize
                    && probeGBuffer0Arr.height == (int) probeGBufferSize
                    && probeGBuffer1Arr.width == (int) probeGBufferSize
                    && probeGBuffer1Arr.height == (int) probeGBufferSize
                    && probeGBuffer2Arr.width == (int) probeGBufferSize
                    && probeGBuffer2Arr.height == (int) probeGBufferSize
                    && probeVBufferArr.width == (int) probeVBufferSize
                    && probeVBufferArr.height == (int) probeVBufferSize;
                if (!probeDataValidInMem) return false;

                probeDataValidInMem = probeGBuffer0Arr.graphicsFormat == probeGBuffer0Format
                    && probeGBuffer1Arr.graphicsFormat == probeGBuffer1Format
                    && probeGBuffer2Arr.graphicsFormat == probeGBuffer2Format
                    && probeVBufferArr.graphicsFormat == probeVBufferFormat;

                return probeDataValidInMem;
            } else {
                probeDataValidInMem = probeVBufferArr && probeIrradianceArr;
                if (!probeDataValidInMem) return false;

                probeDataValidInMem = probeVBufferArr.dimension == TextureDimension.Tex2DArray && probeIrradianceArr.dimension == TextureDimension.Tex2DArray;
                if (!probeDataValidInMem) return false;

                var numProbes = NumProbes;
                probeDataValidInMem = probeVBufferArr.depth == numProbes && probeIrradianceArr.depth == numProbes;
                if (!probeDataValidInMem) return false;

                probeDataValidInMem = probeVBufferArr.width == (int) probeVBufferSize
                    && probeVBufferArr.height == (int) probeVBufferSize
                    && probeIrradianceArr.width == (int) probeIrradianceSize
                    && probeIrradianceArr.height == (int) probeIrradianceSize;
                if (!probeDataValidInMem) return false;

                probeDataValidInMem = probeVBufferArr.graphicsFormat == probeVBufferFormat
                    && probeIrradianceArr.graphicsFormat == probeIrradianceFormat;
                
                return probeDataValidInMem;
            }
        }

        private bool ValidateProbeDataInVram() {
            return false;
        }

        public bool LoadProbeData(bool loadsG0 = true, bool loadsG1 = true, bool loadsG2 = true, bool loadsV = true, bool loadsIrr = false) {
            if (loadsG0) {
                if (!LoadProbeGBuffer0()) return false;
            }
            
            if (loadsG1) {
                if (!LoadProbeGBuffer1()) return false;
            }
            
            if (loadsG2) {
                if (!LoadProbeGBuffer2()) return false;
            }
            
            if (loadsV) {
                if (!LoadProbeVBuffer()) return false;
            }
            
            if (loadsIrr) {
                if (!LoadProbeIrradiance()) return false;
            }
            
            return true;
        }

        public bool LoadProbeGBuffer0() {
            if (probeGBuffer0Arr != null) {
                Destroy(probeGBuffer0Arr);
                probeGBuffer0Arr = null;
            }

            probeGBuffer0Arr = Resources.Load<Texture2DArray>(GetProbeGBuffer0FullPath);

            return probeGBuffer0Arr != null;
        }
        
        public bool LoadProbeGBuffer1() {
            return false;
        }
        
        public bool LoadProbeGBuffer2() {
            return false;
        }
        
        public bool LoadProbeVBuffer() {
            return false;
        }
        
        public bool LoadProbeIrradiance() {
            return false;
        }
    }
    
    public enum DiffuseGIProbeSize {
        _6 = 6,
        _8 = 8,
        _16 = 16,
        _24 = 24,
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024
    }
}