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
        protected DirectionalLight[] _mainLights;

        protected ComputeBuffer _diffuseProbeParamsBuffer;
        protected ComputeBuffer _mainLightBuffer;

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
                PadProbes();
                ReleaseBuffers();
            }
            
            context.Submit();

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

            _mainLights[0] = LightManager.GIMainLightData;
            _mainLightBuffer.SetData(_mainLights);
            _cmd.SetGlobalConstantBuffer(_mainLightBuffer, ShaderKeywordManager.MAIN_LIGHT_DATA, 0, sizeof(DirectionalLight));
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
            var cs = diffuseGISettings.runtimeComputeShader;
            var kernel = cs.FindKernel(ShaderKeywordManager.DIFFUSE_PROBE_RADIANCE_UPDATE);
            
            _cmd.SetComputeTextureParam(cs, kernel, ShaderKeywordManager.RADIANCE_ARRAY, _diffuseProbeRadianceArr);
            _cmd.SetComputeTextureParam(cs, kernel, ShaderKeywordManager.IRRADIANCE_ARRAY, _diffuseProbeIrradianceArr);
            
            var gbufferSize = (int) diffuseGISettings.probeGBufferSize;
            var threadGroupsX = (int) Mathf.Ceil(gbufferSize / 8.0f);
            var threadGroupsY = (int) Mathf.Ceil(gbufferSize / 8.0f);
            var threadGroupsZ = diffuseGISettings.Count;
            
            _cmd.DispatchCompute(cs, kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
            
            _cmd.SetGlobalTexture(ShaderKeywordManager.DIFFUSE_PROBE_RADIANCE_ARRAY, _diffuseProbeRadianceArr);
            ExecuteCommand();
        }

        internal void PrefilterProbes() {
            var cs = diffuseGISettings.runtimeComputeShader;
            var kernel = cs.FindKernel(ShaderKeywordManager.DIFFUSE_PROBE_IRRADIANCE_PREFILTER);

            _cmd.SetComputeTextureParam(cs, kernel, ShaderKeywordManager.RADIANCE_ARRAY, _diffuseProbeRadianceArr);
            _cmd.SetComputeTextureParam(cs, kernel, ShaderKeywordManager.IRRADIANCE_ARRAY, _diffuseProbeIrradianceArr);

            var gbufferSize = (int) diffuseGISettings.probeGBufferSize;
            var threadGroupsX = (int) Mathf.Ceil(gbufferSize / 8.0f);
            var threadGroupsY = (int) Mathf.Ceil(gbufferSize / 8.0f);
            var threadGroupsZ = diffuseGISettings.Count;

            _cmd.DispatchCompute(cs, kernel, threadGroupsX, threadGroupsY, threadGroupsZ);    
            ExecuteCommand();
        }

        internal void PadProbes() {

            var cs = diffuseGISettings.runtimeComputeShader;
            var kernel = cs.FindKernel(ShaderKeywordManager.DIFFUSE_PROBE_IRRADIANCE_PADDING);

            _cmd.SetComputeTextureParam(cs, kernel, ShaderKeywordManager.RADIANCE_ARRAY, _diffuseProbeRadianceArr);
            _cmd.SetComputeTextureParam(cs, kernel, ShaderKeywordManager.IRRADIANCE_ARRAY, _diffuseProbeIrradianceArr);

            var gbufferSize = (int) diffuseGISettings.probeGBufferSize;
            var threadGroupsX = (int) Mathf.Ceil(gbufferSize / 8.0f);
            var threadGroupsY = (int) Mathf.Ceil(gbufferSize / 8.0f);
            var threadGroupsZ = diffuseGISettings.Count;

            _cmd.DispatchCompute(cs, kernel, threadGroupsX, threadGroupsY, threadGroupsZ);

            _cmd.SetGlobalTexture(ShaderKeywordManager.DIFFUSE_PROBE_IRRADIANCE_ARRAY, _diffuseProbeIrradianceArr);
            ExecuteCommand();
        }

        internal void InitBuffers() {
            var s = (int) diffuseGISettings.probeGBufferSize;
            var count = diffuseGISettings.Count;
            
            _historyBuffers.AllocBuffer(ShaderKeywordManager.DIFFUSE_PROBE_IRRADIANCE_ARRAY,
                (system, i) => system.Alloc(size => new Vector2Int(s, s), colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    filterMode: FilterMode.Bilinear, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: count, name: "DiffuseProbeIrradianceArray"), 2);
            
            _historyBuffers.AllocBuffer(ShaderKeywordManager.DIFFUSE_PROBE_RADIANCE_ARRAY,
                (system, i) => system.Alloc(size => new Vector2Int(s, s), colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    filterMode: FilterMode.Bilinear, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: count, name: "DiffuseProbeRadianceArray"), 1);
        }

        internal void InitComputeBuffers() {
            _diffuseProbeParams = new DiffuseProbeParams[1];
            _diffuseProbeParamsBuffer = new ComputeBuffer(1, sizeof(DiffuseProbeParams), ComputeBufferType.Constant);

            _mainLights = new DirectionalLight[1];
            _mainLightBuffer = new ComputeBuffer(1, sizeof(DirectionalLight), ComputeBufferType.Constant);
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
            _mainLightBuffer?.Dispose();
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