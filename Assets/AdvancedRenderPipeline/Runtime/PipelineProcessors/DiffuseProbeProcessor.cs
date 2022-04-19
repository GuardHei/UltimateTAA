using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.PipelineProcessors {
    public unsafe class DiffuseProbeProcessor : PipelineProcessor {

        public static DiffuseGISettings diffuseGISettings => AdvancedRenderPipeline.settings.diffuseGISettings;

        #region Compute Buffers

        protected DiffuseProbeParams[] _diffuseProbeParams;

        protected ComputeBuffer _diffuseProbeParamsBuffer;

        #endregion

        public DiffuseProbeProcessor() {
            _processorDesc = "Process Diffuse Probes";
            InitComputeBuffers();
        }

        public override void Process(ScriptableRenderContext context) {
            _context = context;
            _cmd = CommandBufferPool.Get(_processorDesc);

            Setup();

            if (diffuseGISettings.Enabled) {
                RelightProbes();
                PrefilterProbes();
            }

            DisposeCommandBuffer();
        }

        internal void Setup() {

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

        public void InitComputeBuffers() {
            _diffuseProbeParams = new DiffuseProbeParams[1];
            _diffuseProbeParamsBuffer = new ComputeBuffer(1, sizeof(DiffuseProbeParams), ComputeBufferType.Constant);
        }

        public void ReleaseComputeBuffers() {
            _diffuseProbeParamsBuffer?.Dispose();
        }
        
        public override void Dispose() {
            DisposeCommandBuffer();
            ReleaseComputeBuffers();
        }
    }
}