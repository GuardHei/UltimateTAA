using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Vector2 = UnityEngine.Vector2;

namespace AdvancedRenderPipeline.Runtime.Cameras {
    public abstract class CameraRenderer : IDisposable {

        public static AdvancedRenderPipelineSettings settings => AdvancedRenderPipeline.settings;

        public Camera camera;
        public AdvancedCameraType cameraType;

        public Vector2Int OutputRes {
            get => _outputRes;
            set {
                if (_outputRes == value) return;
                _outputRes = value;
                UpdateRenderScale();
            }
        }

        public Vector2Int InternalRes => _internalRes;

        public Vector2 Ratio {
            get => _ratio;
            set {
                if (_ratio == value || value.x > 1 || value.y > 1) return;
                _ratio = value;
                UpdateRenderScale(false);
            }
        }

        public bool IsOnFirstFrame => _frameNum == 1; // default is 0, and we start at 1.

        protected Vector2Int _outputRes;
        protected Vector2Int _internalRes;
        protected Vector2 _ratio;
        protected Vector2Int _lastRenderOutputRes;
        protected Vector2 _lastRenderRatio;
        
        protected ScriptableRenderContext _context; // Not persistent
        protected CommandBuffer _cmd; // current active command buffer
        protected CullingResults _cullingResults;

        protected int _frameNum;
        protected float3 _cameraPosWS;
        protected float3 _cameraFwdWS;
        protected float3 _cameraUpWS;
        protected float3 _cameraRightWS;
        protected Matrix4x4 _frustumCornersWS;
        protected Matrix4x4 _prevMatrixVP;
        protected Matrix4x4 _prevInvMatrixVP;

        public abstract void Render(ScriptableRenderContext context);

        public abstract void Setup();

        public virtual void PreUpdate() {
            _frameNum++;
            _lastRenderOutputRes = OutputRes;
            _lastRenderRatio = Ratio;
        }

        public virtual void ResetFrameHistory() {
            _frameNum = 0;
        }

        #region Command Buffer Utils

        public void DisposeCommandBuffer() {
            if (_cmd != null) {
                CommandBufferPool.Release(_cmd);
                _cmd = null;
            }
        }
        
        public void SetRenderTarget(RTHandle colorBuffer, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, 0, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, int mipLevel, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, mipLevel, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, int mipLevel, CubemapFace cubemapFace, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, mipLevel, cubemapFace, 0);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, int mipLevel, CubemapFace cubemapFace, int depthSlice,
            bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, mipLevel, cubemapFace, depthSlice);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, RTHandle depthBuffer, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, depthBuffer, 0, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, RTHandle depthBuffer, int mipLevel, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, depthBuffer, mipLevel, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, RTHandle depthBuffer, int mipLevel, CubemapFace cubemapFace,
            bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, depthBuffer, mipLevel, cubemapFace, 0);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, RTHandle depthBuffer, int mipLevel, CubemapFace cubemapFace,
            int depthSlice, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, depthBuffer, mipLevel, cubemapFace, depthSlice);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle colorBuffer, RenderBufferLoadAction colorLoad,
            RenderBufferStoreAction colorStore, RTHandle depthBuffer, RenderBufferLoadAction depthLoad,
            RenderBufferStoreAction depthStore, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffer, colorLoad, colorStore, depthBuffer, depthLoad, depthStore);
            CoreUtils.SetViewport(_cmd, colorBuffer);
            if (clear) ClearRenderTarget(RTClearFlags.All);
        }

        public void SetRenderTarget(RTHandle[] colorBuffers, RTHandle depthBuffer, bool clear = false) {
            _cmd.SetRenderTarget(ARPUtils.RTHandlesToRTIs(colorBuffers), depthBuffer, 0, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }

        public void SetRenderTarget(RTHandle[] colorBuffers, RTHandle depthBuffer, int mipLevel, bool clear = false) {
            _cmd.SetRenderTarget(ARPUtils.RTHandlesToRTIs(colorBuffers), depthBuffer, mipLevel, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }

        public void SetRenderTarget(RTHandle[] colorBuffers, RTHandle depthBuffer, int mipLevel,
            CubemapFace cubemapFace, bool clear = false) {
            _cmd.SetRenderTarget(ARPUtils.RTHandlesToRTIs(colorBuffers), depthBuffer, mipLevel, cubemapFace, 0);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }

        public void SetRenderTarget(RTHandle[] colorBuffers, RTHandle depthBuffer, int mipLevel,
            CubemapFace cubemapFace, int depthSlice, bool clear = false) {
            _cmd.SetRenderTarget(ARPUtils.RTHandlesToRTIs(colorBuffers), depthBuffer, mipLevel, cubemapFace,
                depthSlice);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }
        
        public void SetRenderTarget(RTHandle refColor, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffers, depthBuffer, 0, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, refColor);
        }

        public void SetRenderTarget(RTHandle refColor, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, int mipLevel, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffers, depthBuffer, mipLevel, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, refColor);
        }

        public void SetRenderTarget(RTHandle refColor, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, int mipLevel,
            CubemapFace cubemapFace, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffers, depthBuffer, mipLevel, cubemapFace, 0);
            CoreUtils.SetViewport(_cmd, refColor);
        }

        public void SetRenderTarget(RTHandle refColor, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, int mipLevel,
            CubemapFace cubemapFace, int depthSlice, bool clear = false) {
            _cmd.SetRenderTarget(colorBuffers, depthBuffer, mipLevel, cubemapFace, depthSlice);
            CoreUtils.SetViewport(_cmd, refColor);
        }
        
        public void SetRenderTargetNonAlloc(RTHandle[] colorBuffers, RenderTargetIdentifier[] rts, RTHandle depthBuffer, bool clear = false) {
            ARPUtils.RTHandlesToRTIsNonAlloc(colorBuffers, ref rts);
            _cmd.SetRenderTarget(rts, depthBuffer, 0, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }

        public void SetRenderTargetNonAlloc(RTHandle[] colorBuffers, RenderTargetIdentifier[] rts, RTHandle depthBuffer, int mipLevel, bool clear = false) {
            ARPUtils.RTHandlesToRTIsNonAlloc(colorBuffers, ref rts);
            _cmd.SetRenderTarget(rts, depthBuffer, mipLevel, CubemapFace.Unknown, 0);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }

        public void SetRenderTargetNonAlloc(RTHandle[] colorBuffers, RenderTargetIdentifier[] rts, RTHandle depthBuffer, int mipLevel,
            CubemapFace cubemapFace, bool clear = false) {
            ARPUtils.RTHandlesToRTIsNonAlloc(colorBuffers, ref rts);
            _cmd.SetRenderTarget(rts, depthBuffer, mipLevel, cubemapFace, 0);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }

        public void SetRenderTargetNonAlloc(RTHandle[] colorBuffers, RenderTargetIdentifier[] rts, RTHandle depthBuffer, int mipLevel,
            CubemapFace cubemapFace, int depthSlice, bool clear = false) {
            ARPUtils.RTHandlesToRTIsNonAlloc(colorBuffers, ref rts);
            _cmd.SetRenderTarget(rts, depthBuffer, mipLevel, cubemapFace, depthSlice);
            CoreUtils.SetViewport(_cmd, colorBuffers[0]);
        }

        public void ClearRenderTarget(RTClearFlags flags) => ClearRenderTarget(flags, Color.black);

        public void ClearRenderTarget(RTClearFlags flags, Color color, float depth = 1f, uint stencil = 0) =>
            _cmd.ClearRenderTarget(flags, color, depth, stencil);

        public void ClearRenderTarget(bool clearColor, bool clearDepth) =>
            ClearRenderTarget(clearColor, clearDepth, Color.black);

        public void ClearRenderTarget(bool clearColor, bool clearDepth, Color color, float depth = 1f) =>
            _cmd.ClearRenderTarget(clearDepth, clearColor, color, depth);

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

        public void Submit() => _context.Submit();

        protected virtual void UpdateRenderScale(bool outputChanged = true) => _internalRes = Vector2Int.CeilToInt(OutputRes * Ratio);

        public void SetResolutionAndRatio(int w, int h, float x, float y) {
            var prevOutput = _lastRenderOutputRes;
            var prevRatio = _lastRenderRatio;
            
            _outputRes = new Vector2Int(w, h);
            _ratio = new Vector2(x, y);

            var outputChanged = prevOutput != _outputRes;
            var ratioChanged = prevRatio != _ratio;

            UpdateRenderScale(outputChanged);
        }

        public CameraRenderer(Camera camera) {
            this.camera = camera;
            camera.forceIntoRenderTexture = true;
        }

        public virtual void Dispose() {
            camera = null;
            DisposeCommandBuffer();
        }

        public static CameraRenderer CreateCameraRenderer(Camera camera, AdvancedCameraType type) {
            switch (type) {
                case AdvancedCameraType.Game: return new GameCameraRenderer(camera);
                case AdvancedCameraType.Reflection: return new GameCameraRenderer(camera);
#if UNITY_EDITOR
                case AdvancedCameraType.SceneView: return new SceneViewCameraRenderer(camera);
                case AdvancedCameraType.Preview: return new GameCameraRenderer(camera);
#endif
                default: throw new InvalidOperationException("Does not support camera type: " + type);
            }
        }

        public static AdvancedCameraType DefaultToAdvancedCameraType(CameraType cameraType) {
            switch (cameraType) {
                case CameraType.Game: return AdvancedCameraType.Game;
#if UNITY_EDITOR
                case CameraType.SceneView: return AdvancedCameraType.SceneView;
                case CameraType.Preview: return AdvancedCameraType.Preview;
#endif
                case CameraType.VR: return AdvancedCameraType.VR;
                case CameraType.Reflection: return AdvancedCameraType.Reflection;
                default: throw new InvalidOperationException("Does not support camera type: " + cameraType);
            }
        }
    }

    public enum AdvancedCameraType {
        Game = 1,
        SceneView = 2,
        Preview = 4,
        VR = 8,
        Reflection = 16
    }
}
