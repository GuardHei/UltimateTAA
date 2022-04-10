using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.Cameras {
    public unsafe class DiffuseProbeCameraRenderer : CameraRenderer {

        public static readonly Vector3[] CubemapEulerAngles = {
            new(0.0f,-90.0f,0.0f), // +X
            new(0.0f,90.0f,0.0f), // -X
            new(-90.0f,0.0f,0.0f), // +Y
            new(90.0f,0.0f,0.0f), // -Y
            new(0.0f,0.0f,0.0f), // +Z
            new(0.0f,180.0f,0.0f) // -Z
        };

        protected string _rendererDesc;
        protected ARPCameraAdditionData _additionalData;

        // GBuffer Layout
        // GBuffer 0 (RGBA8): RGB - Albedo, A - Sky Visibility
        // GBuffer 1 (RG8): RG - Normal (Octhedron Encoded)
        // GBuffer 2 (RG16): R - Radial Distance, G - (Radial Distance) ^ 2
        protected Texture _gbuffer0;
        protected Texture _gbuffer1;
        protected Texture _gbuffer2;

        protected RenderTargetIdentifier[] _gbufferMRT = new RenderTargetIdentifier[3];
        protected RenderTargetIdentifier _depthTex = new(ShaderKeywordManager.DEPTH_TEXTURE);

        protected int _currentFace = 0;
        
        #region Compute Buffers

        protected CameraData[] _cameraData;

        protected ComputeBuffer _cameraDataBuffer;

        #endregion

        public DiffuseProbeCameraRenderer(Camera camera) : base(camera) {
            cameraType = AdvancedCameraType.DiffuseProbe;
            _rendererDesc = "Render Diffuse Probe (" + camera.name + ")";
            _additionalData = camera.GetComponent<ARPCameraAdditionData>();
            InitComputeBuffers();
        }

        public override void Render(ScriptableRenderContext context) {
	        _gbuffer0 = _additionalData.diffuseProbeGBuffer0;
	        _gbuffer1 = _additionalData.diffuseProbeGBuffer1;
	        _gbuffer2 = _additionalData.diffuseProbeGBuffer2;
	        
	        if (!_gbuffer0 || !_gbuffer1 || !_gbuffer2) return;

	        _context = context;
            _cmd = CommandBufferPool.Get(_rendererDesc);

            GetBuffers();

            for (_currentFace = 0; _currentFace < 6; _currentFace++) {
                Setup();
                Cull();
                DrawGBufferPass();
                FinalBlit();
                Submit();
            }

            ReleaseBuffers();

            DisposeCommandBuffer();
        }
        
        public override void Setup() {
            camera.aspect = 1f;
            camera.fieldOfView = 90f;
            
            var transform = camera.transform;
            transform.eulerAngles = CubemapEulerAngles[_currentFace];
            
            _context.SetupCameraProperties(camera);
            
            var cameraViewMatrix = camera.worldToCameraMatrix;
			
            _cameraPosWS = transform.position;
            _cameraFwdWS = cameraViewMatrix.GetViewForward();
            _cameraUpWS = cameraViewMatrix.GetViewUp();
            _cameraRightWS = cameraViewMatrix.GetViewRight();

            var screenSize = Vector4.one;
            screenSize.x = InternalRes.x;
            screenSize.y = InternalRes.y;
            screenSize.z = 1.0f / InternalRes.x;
            screenSize.w = 1.0f / InternalRes.y;

            Vector2Int viewportSize = new (camera.pixelWidth, camera.pixelHeight);

            var rtProps = new PackedRTHandleProperties {
                viewportSize = new int4(ARPUtils.Vector2IntToInt4(viewportSize, viewportSize)),
                rtSize = new int4(ARPUtils.Vector2IntToInt4(InternalRes, InternalRes)),
                rtHandleScale = new float4(1f, 1f, 1f, 1f)
            };
            
            var viewMatrix = camera.worldToCameraMatrix;
			_matrixVP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * viewMatrix;
			_invMatrixVP = _matrixVP.inverse;
			_nonjitteredMatrixVP = GL.GetGPUProjectionMatrix(camera.nonJitteredProjectionMatrix, false) * viewMatrix;
			_invNonJitteredMatrixVP = _nonjitteredMatrixVP.inverse;

			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_MATRIX_I_VP, _invMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_PREV_MATRIX_VP, IsOnFirstFrame ? _nonjitteredMatrixVP : _prevMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_PREV_MATRIX_I_VP, IsOnFirstFrame ? _invNonJitteredMatrixVP : _prevInvMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_MATRIX_NONJITTERED_VP, _nonjitteredMatrixVP);
			_cmd.SetGlobalMatrix(ShaderKeywordManager.UNITY_MATRIX_NONJITTERED_I_VP, _invNonJitteredMatrixVP);
			
			var farHalfFovTan = _farPlane * _verticalFovTan;
			
			_prevFrustumCornersWS = _frustumCornersWS;

			_frustumCornersWS = new Matrix4x4();
			
			var fwdDir = _cameraFwdWS * _farPlane;
			var upDir = _cameraUpWS * farHalfFovTan;
			var rightDir = _cameraRightWS * farHalfFovTan * _aspect;
			
			var topLeft = fwdDir + upDir - rightDir;
			var topRight = fwdDir + upDir + rightDir * 3f;
			var bottomLeft = fwdDir - upDir * 3f - rightDir;
			// var bottomRight = fwdDir - upDir * 3f + rightDir * 3f;

			var zBufferParams = new float4((_farPlane - _nearPlane) / _nearPlane,  1f, (_farPlane - _nearPlane) / (_nearPlane * _farPlane), 1f / _farPlane);

			_frustumCornersWS.SetRow(0, new float4(topLeft, .0f));
			_frustumCornersWS.SetRow(1, new float4(bottomLeft, .0f));
			_frustumCornersWS.SetRow(2, new float4(topRight, .0f));
			_frustumCornersWS.SetRow(3, zBufferParams);

			_cameraData[0] = new CameraData {
				cameraPosWS = new float4(_cameraPosWS, .0f),
				cameraFwdWS = new float4(_cameraFwdWS, 1.0f),
				screenSize = screenSize,
				frustumCornersWS = _frustumCornersWS,
				prevFrustumCornersWS = _prevFrustumCornersWS,
				_rtHandleProps = rtProps
			};
			
			// Debug.Log(rtProps.rtHandleScale);
			
			_cameraDataBuffer.SetData(_cameraData);
			
			_cmd.SetGlobalConstantBuffer(_cameraDataBuffer, ShaderKeywordManager.CAMERA_DATA, 0, sizeof(CameraData));
			
			ExecuteCommand();
        }

        public void Cull() {
            if (!camera.TryGetCullingParameters(out var cullingParameters)) {
                Debug.Log("Culling Failed for " + _rendererDesc);
                return;
            }

            _cullingResults = _context.Cull(ref cullingParameters);
        }

        public void DrawGBufferPass() {
	        _gbufferMRT[0] = _gbuffer0;
	        _gbufferMRT[1] = _gbuffer1;
	        _gbufferMRT[2] = _gbuffer2;

	        _cmd.SetRenderTarget(_gbufferMRT, _depthTex, 0, (CubemapFace) _currentFace, 0);
	        _cmd.ClearRenderTarget(true, true, Color.black, 1f);
	        
	        ExecuteCommand();
	        
	        var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque | SortingCriteria.OptimizeStateChanges | SortingCriteria.QuantizedFrontToBack };
	        var drawSettings = new DrawingSettings(ShaderTagManager.DIFFUSE_PROBE_GBUFFER, sortingSettings) { enableInstancing = settings.enableAutoInstancing };
	        var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
			
	        _context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
        }

        public void FinalBlit() {
            
        }
        
        public void GetBuffers() {
            _cmd.GetTemporaryRT(ShaderKeywordManager.DEPTH_TEXTURE, InternalRes.x, InternalRes.y, 24, FilterMode.Point, RenderTextureFormat.Depth);
        }

        public void ReleaseBuffers() {
            _cmd.ReleaseTemporaryRT(ShaderKeywordManager.DEPTH_TEXTURE);
        }
        
        public void InitComputeBuffers() {
	        _cameraData = new CameraData[1];
	        _cameraDataBuffer = new ComputeBuffer(1, sizeof(CameraData), ComputeBufferType.Constant);
        }

        public void ReleaseComputeBuffers() {
	        _cameraDataBuffer.Dispose();
        }
        
        public override void Dispose() {
	        ReleaseComputeBuffers();
			
	        base.Dispose();
        }
    }
}