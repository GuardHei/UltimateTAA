using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.PipelineProcessors {
    public unsafe class DiffuseProbeProcessor : PipelineProcessor {

        public static DiffuseGISettings diffuseGISettings => AdvancedRenderPipeline.settings.diffuseGISettings;
        
        #region RT Handles & Render Target Identifiers
        
        protected readonly BufferedRTHandleSystem _historyBuffers = new();

        protected RTHandle _diffuseProbeRadianceArr;
        protected RTHandle _diffuseProbeIrradianceArr;
        protected RTHandle _prevDiffuseProbeIrradianceArr;
        
        #endregion

        #region Compute Buffers

        protected DiffuseProbeParams[] _diffuseProbeParams;

        protected ComputeBuffer _diffuseProbeParamsBuffer;

        #endregion

        public DiffuseProbeProcessor() {
            _processorDesc = "Process Diffuse Probes";
            InitBuffers();
            InitComputeBuffers();
        }

        public override void Process(ScriptableRenderContext context) {
            _context = context;
            _cmd = CommandBufferPool.Get(_processorDesc);

            Setup();

            if (diffuseGISettings.Enabled) {
                GetBuffers();
                RelightProbes();
                PrefilterProbes();
                ReleaseBuffers();
            }

            DisposeCommandBuffer();
        }

        internal void Setup() {
            
            SetupGILights();

            var valid = ValidateProbeData();
            
            var giSettings = diffuseGISettings;

            if (valid) {
                giSettings.enabledFlag = true;
                
                var needsUpdate = diffuseGISettings.markProbesDirty;
                needsUpdate |= AdvancedRenderPipeline.instance.IsOnFirstFrame;
                if (needsUpdate) {
                    SetupProbeBuffers();
                    giSettings.markProbesDirty = false;
                }
            } else giSettings.enabledFlag = false;
            
            AdvancedRenderPipeline.settings.diffuseGISettings = giSettings;
            
            var diffuseProbeParams = diffuseGISettings.GPUParams;
            _diffuseProbeParams[0] = diffuseProbeParams;
            _diffuseProbeParamsBuffer.SetData(_diffuseProbeParams);
            _cmd.SetGlobalConstantBuffer(_diffuseProbeParamsBuffer, ShaderKeywordManager.DIFFUSE_PROBE_PARAMS, 0, sizeof(DiffuseProbeParams));

            ExecuteCommand();
        }

        internal void SetupGILights() {
            var mainLight = RenderSettings.sun;
            LightManager.GIMainLight = mainLight;
        }

        internal bool ValidateProbeData() {

            var count = diffuseGISettings.Count;

            var gb0 = diffuseGISettings.probeGBufferArr0;
            var gb1 = diffuseGISettings.probeGBufferArr1;
            var gb2 = diffuseGISettings.probeGBufferArr2;
            var vb0 = diffuseGISettings.probeVBufferArr0;

            if (!gb0 || gb0.depth != count) return false;
            if (!gb1 || gb1.depth != count) return false;
            if (!gb2 || gb2.depth != count) return false;
            if (!vb0 || vb0.depth != count) return false;
            
            return true;
        }

        internal void SetupProbeBuffers() {
            _cmd.SetGlobalTexture(ShaderKeywordManager.DIFFUSE_PROBE_GBUFFER_ARRAY_0, diffuseGISettings.probeGBufferArr0);
            _cmd.SetGlobalTexture(ShaderKeywordManager.DIFFUSE_PROBE_GBUFFER_ARRAY_1, diffuseGISettings.probeGBufferArr1);
            _cmd.SetGlobalTexture(ShaderKeywordManager.DIFFUSE_PROBE_GBUFFER_ARRAY_2, diffuseGISettings.probeGBufferArr2);
            _cmd.SetGlobalTexture(ShaderKeywordManager.DIFFUSE_PROBE_VBUFFER_ARRAY_0, diffuseGISettings.probeVBufferArr0);
        }

        internal void RelightProbes() {
            
        }

        internal void PrefilterProbes() {
            
        }

        internal void InitBuffers() {
            var s = (int) diffuseGISettings.probeGBufferSize;
            
            _historyBuffers.AllocBuffer(ShaderKeywordManager.DIFFUSE_PROBE_IRRADIANCE_ARRAY,
                (system, i) => system.Alloc(size => new Vector2Int(s, s), colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    filterMode: FilterMode.Bilinear, name: "DiffuseProbeIrradianceArray"), 2);
            
            _historyBuffers.AllocBuffer(ShaderKeywordManager.DIFFUSE_PROBE_RADIANCE_ARRAY,
                (system, i) => system.Alloc(size => new Vector2Int(s, s), colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    filterMode: FilterMode.Bilinear, name: "DiffuseProbeRadianceArray"), 1);
        }

        internal void InitComputeBuffers() {
            _diffuseProbeParams = new DiffuseProbeParams[1];
            _diffuseProbeParamsBuffer = new ComputeBuffer(1, sizeof(DiffuseProbeParams), ComputeBufferType.Constant);
        }

        internal void GetBuffers() {
            var s = (int) diffuseGISettings.probeGBufferSize;
            _historyBuffers.SwapAndSetReferenceSize(s, s);

            _diffuseProbeIrradianceArr = _historyBuffers.GetFrameRT(ShaderKeywordManager.DIFFUSE_PROBE_IRRADIANCE_ARRAY, 0);
            _diffuseProbeRadianceArr = _historyBuffers.GetFrameRT(ShaderKeywordManager.DIFFUSE_PROBE_RADIANCE_ARRAY, 0);
            
            _prevDiffuseProbeIrradianceArr = _historyBuffers.GetFrameRT(ShaderKeywordManager.DIFFUSE_PROBE_IRRADIANCE_ARRAY, 1);
        }
        
        internal void ReleaseBuffers() { }

        internal void ReleaseComputeBuffers() {
            _diffuseProbeParamsBuffer?.Dispose();
        }
        
        public override void Dispose() {
            if (_historyBuffers != null) {
                _historyBuffers.ReleaseAll();
                _historyBuffers.Dispose();
            }
            
            DisposeCommandBuffer();
            ReleaseBuffers();
            ReleaseComputeBuffers();
        }
    }
}