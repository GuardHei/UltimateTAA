using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
    public unsafe class DiffuseProbeProcessor : IDisposable {

        public static AdvancedRenderPipelineSettings settings => AdvancedRenderPipeline.settings;

        public static DiffuseGISettings diffuseGISettings => AdvancedRenderPipeline.settings.diffuseGISettings;

        public string _processorDesc;

        protected ScriptableRenderContext _context; // Not persistent
        protected CommandBuffer _cmd; // current active command buffer

        #region Compute Buffers

        protected DiffuseProbeParams[] _diffuseProbeParams;

        protected ComputeBuffer _diffuseProbeParamsBuffer;

        #endregion

        public DiffuseProbeProcessor() {
            _processorDesc = "Process Diffuse Probes";
            InitComputeBuffers();
        }

        public void Process(ScriptableRenderContext context) {
            _context = context;
            _cmd = CommandBufferPool.Get(_processorDesc);

            Setup();

            DisposeCommandBuffer();
        }

        internal void Setup() {
            var diffuseProbeParams = diffuseGISettings.GPUParams;
            _diffuseProbeParams[0] = diffuseProbeParams;
            _diffuseProbeParamsBuffer.SetData(_diffuseProbeParams);
            _cmd.SetGlobalConstantBuffer(_diffuseProbeParamsBuffer, ShaderKeywordManager.DIFFUSE_PROBE_PARAMS, 0, sizeof(DiffuseProbeParams));
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        public void DisposeCommandBuffer() {
            if (_cmd != null) {
                CommandBufferPool.Release(_cmd);
                _cmd = null;
            }
        }
        
        public void InitComputeBuffers() {
            _diffuseProbeParams = new DiffuseProbeParams[1];
            _diffuseProbeParamsBuffer = new ComputeBuffer(1, sizeof(DiffuseProbeParams), ComputeBufferType.Constant);
        }

        public void ReleaseComputeBuffers() {
            _diffuseProbeParamsBuffer?.Dispose();
        }
        
        public void Dispose() {
            DisposeCommandBuffer();
            ReleaseComputeBuffers();
        }
    }
}