using System;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Matrix4x4 = UnityEngine.Matrix4x4;
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
        protected float _fov;
        protected float _aspect;
        protected float _nearPlane;
        protected float _farPlane;
        protected float _verticalFovTan;
        protected float3 _cameraPosWS;
        protected float3 _cameraFwdWS;
        protected float3 _cameraUpWS;
        protected float3 _cameraRightWS;
        protected Matrix4x4 _frustumCornersWS;
        protected Matrix4x4 _prevFrustumCornersWS;
        protected Vector2[] _jitterPatterns = new Vector2[8];
        protected Vector2 _currJitter;
        protected Matrix4x4 _nonjitteredMatrixVP;
        protected Matrix4x4 _invNonJitteredMatrixVP;
        protected Matrix4x4 _matrixVP;
        protected Matrix4x4 _invMatrixVP;
        protected Matrix4x4 _prevMatrixVP; // nonjittered
        protected Matrix4x4 _prevInvMatrixVP; // nonjittered

        public abstract void Render(ScriptableRenderContext context);

        public abstract void Setup();

        public virtual void PreUpdate() {
            _frameNum++;
            _lastRenderOutputRes = OutputRes;
            _lastRenderRatio = Ratio;
            _aspect = camera.aspect;
            _nearPlane = camera.nearClipPlane;
            _farPlane = camera.farClipPlane;
            _fov = camera.fieldOfView;
            _verticalFovTan = Mathf.Tan(.5f * Mathf.Deg2Rad * _fov);
        }

        public virtual void PostUpdate() {
            _prevMatrixVP = _nonjitteredMatrixVP;
            _prevInvMatrixVP = _invNonJitteredMatrixVP;
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

            for (var i = 0; i < (int) settings.taaSettings.jitterNum; i++) {
                _jitterPatterns[i] = new Vector2(HaltonSequence.Get((i & 1023) + 1, 2) - .5f, HaltonSequence.Get((i & 1023) + 1, 3) - .5f);
                // _jitterPatterns[i] = new Vector2(HaltonSequence.Get((i & 1023) + 1, 2), HaltonSequence.Get((i & 1023) + 1, 3));
            }
        }

        public virtual void Dispose() {
            camera = null;
            DisposeCommandBuffer();
        }

        public void ConfigureProjectionMatrix(Vector2 jitter) {
            camera.ResetProjectionMatrix();
            camera.nonJitteredProjectionMatrix = camera.projectionMatrix;
            camera.projectionMatrix = GetJitteredProjectionMatrix(jitter);
            camera.useJitteredProjectionMatrixForTransparentRendering = true;
        }

        public Matrix4x4 GetJitteredProjectionMatrix(Vector2 jitter) => camera.orthographic ? GetJitteredOrthographicProjectionMatrix(jitter) : GetJitteredPerspectiveProjectionMatrix(jitter);

        public Matrix4x4 GetJitteredOrthographicProjectionMatrix(Vector2 jitter) {
            var vertical = camera.orthographicSize;
            var horizontal = vertical * _aspect;

            jitter.x *= horizontal / (.5f * InternalRes.x);
            jitter.y *= vertical / (.5f * InternalRes.y);

            var left = jitter.x - horizontal;
            var right = jitter.x + horizontal;
            var top = jitter.y + vertical;
            var bottom = jitter.y - vertical;

            return Matrix4x4.Ortho(left, right, bottom, top, _nearPlane, _farPlane);
        }

        public Matrix4x4 GetJitteredPerspectiveProjectionMatrix(Vector2 jitter) {
            var vertical = _verticalFovTan * _nearPlane;
            var horizontal = vertical * _aspect;
            
            /*
            jitter.x *= horizontal / (.5f * InternalRes.x);
            jitter.y *= vertical / (.5f * InternalRes.y);

            var proj = camera.projectionMatrix;
            proj.m02 += jitter.x / horizontal;
            proj.m12 += jitter.y / vertical;
            */
            
            jitter.x *= 1f / (.5f * InternalRes.x);
            jitter.y *= 1f / (.5f * InternalRes.y);

            var proj = camera.projectionMatrix;
            proj.m02 += jitter.x;
            proj.m12 += jitter.y;

            return proj;
        }

        public static CameraRenderer CreateCameraRenderer(Camera camera, AdvancedCameraType type) {
            switch (type) {
                case AdvancedCameraType.Game: return new GameCameraRenderer(camera);
                case AdvancedCameraType.Reflection: return new GameCameraRenderer(camera);
#if UNITY_EDITOR
                case AdvancedCameraType.SceneView: return new SceneViewCameraRenderer(camera);
                case AdvancedCameraType.Preview: return new PreviewCameraRenderer(camera);
                case AdvancedCameraType.DiffuseProbe: return new DiffuseProbeCameraRenderer(camera);
#endif
                default: throw new InvalidOperationException("Does not support camera type: " + type);
            }
        }

        public static AdvancedCameraType GetCameraType(Camera camera) {
#if UNITY_EDITOR
            if (camera.TryGetComponent<ARPCameraAdditionalData>(out var additionData)) return additionData.cameraType;
#endif
            return DefaultToAdvancedCameraType(camera.cameraType);
        }

        public static AdvancedCameraType DefaultToAdvancedCameraType(CameraType cameraType) {
            switch (cameraType) {
                case CameraType.Game: return AdvancedCameraType.Game;
#if UNITY_EDITOR
                case CameraType.SceneView: return AdvancedCameraType.SceneView;
                case CameraType.Preview: return AdvancedCameraType.Preview;
#endif
                case CameraType.VR: return AdvancedCameraType.VR;
                case CameraType.Reflection: return AdvancedCameraType.Game;
                default: throw new InvalidOperationException("Does not support camera type: " + cameraType);
            }
        }
    }

    public enum AdvancedCameraType {
        Game = 1,
        SceneView = 2,
        Preview = 4,
        VR = 8,
        Reflection = 16,
        DiffuseProbe = 32
    }
}
