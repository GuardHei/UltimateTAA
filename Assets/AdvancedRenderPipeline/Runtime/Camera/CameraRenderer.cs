using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime {
    public unsafe abstract class CameraRenderer : IDisposable {

        public static AdvancedRenderPipelineSettings settings => AdvancedRenderPipeline.settings;

        public Camera camera;
        public AdvancedCameraType cameraType;

        public int outputWidth;
        public int outputHeight;
        public float xRatio = 0f;
        public float yRatio = 0f;

        public Vector2 ratio => new Vector2(xRatio, yRatio);

        public int internalWidth => Mathf.CeilToInt(outputWidth * xRatio);

        public int internalHeight => Mathf.CeilToInt(outputHeight * yRatio);

        public Vector2Int internalRes => new Vector2Int(internalWidth, internalHeight);

        internal ScriptableRenderContext _context; // Not persistent
        internal CommandBuffer _cmd; // current active command buffer
        internal CullingResults _cullingResults;

        public abstract void Render(ScriptableRenderContext context);

        public abstract void Setup();

        #region Command Buffer Utils

        public void DisposeCMD() {
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

        public virtual void SetResolution(int w, int h) {
            outputWidth = w;
            outputHeight = h;
        }

        public virtual void SetRatio(float x, float y) {
            xRatio = x;
            yRatio = y;
        }

        public virtual void SetResolutionAndRatio(int w, int h, float x, float y) {
            outputWidth = w;
            outputHeight = h;
            xRatio = x;
            yRatio = y;
        }

        public CameraRenderer(Camera camera) {
            this.camera = camera;
            camera.forceIntoRenderTexture = true;
        }

        public virtual void Dispose() {
            camera = null;
            DisposeCMD();
        }

        public static CameraRenderer CreateCameraRenderer(Camera camera, AdvancedCameraType type) {
            switch (type) {
                case AdvancedCameraType.Game: return new GameCameraRenderer(camera);
                case AdvancedCameraType.SceneView: return new SceneViewCameraRenderer(camera);
                case AdvancedCameraType.Preview: return new GameCameraRenderer(camera);
                default: throw new InvalidOperationException("Does not support camera type: " + type);
            }
        }

        public static AdvancedCameraType DefaultToAdvancedCameraType(CameraType cameraType) {
            switch (cameraType) {
                case CameraType.Game: return AdvancedCameraType.Game;
                case CameraType.SceneView: return AdvancedCameraType.SceneView;
                case CameraType.Preview: return AdvancedCameraType.Preview;
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
