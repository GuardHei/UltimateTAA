using System;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.PipelineProcessors {
    public abstract class PipelineProcessor : IDisposable {
    
        protected string _processorDesc;

        protected ScriptableRenderContext _context; // Not persistent
        protected CommandBuffer _cmd; // current active command buffer

        public abstract void Process(ScriptableRenderContext context);

        public abstract void Dispose();
        
        public void DisposeCommandBuffer() {
            if (_cmd != null) {
                CommandBufferPool.Release(_cmd);
                _cmd = null;
            }
        }

        #region Command Buffer Utils
        
        public void BeginSample(String name) {
#if UNITY_EDITOR || DEBUG
            _cmd.BeginSample(name);
            // ExecuteCommand(); // Don't really have to.
#endif
        }

        public void EndSample(String name) {
#if UNITY_EDITOR || DEBUG
            _cmd.EndSample(name);
            ExecuteCommand();
#endif
        }

        public void ExecuteCommand(bool clear = true) {
            _context.ExecuteCommandBuffer(_cmd);
            if (clear) _cmd.Clear();
        }

        public void ExecuteCommand(CommandBuffer buffer, bool clear = true) {
            _context.ExecuteCommandBuffer(buffer);
            if (clear) buffer.Clear();
        }

        public void ExecuteCommandAsync(ComputeQueueType queueType, bool clear = true) {
            _context.ExecuteCommandBufferAsync(_cmd, queueType);
            if (clear) _cmd.Clear();
        }

        public void ExecuteCommandAsync(CommandBuffer buffer, ComputeQueueType queueType, bool clear = true) {
            _context.ExecuteCommandBufferAsync(buffer, queueType);
            if (clear) buffer.Clear();
        }
        
        #endregion
    }
}