using System;
using UnityEngine;

namespace AdvancedRenderPipeline.Runtime.Cameras {
    
    [RequireComponent(typeof(Camera))]
    public class ARPCameraAdditionalData : MonoBehaviour {
        public AdvancedCameraType cameraType = AdvancedCameraType.Game;
        
        public RenderTexture diffuseProbeGBuffer0;
        public RenderTexture diffuseProbeGBuffer1;
        public RenderTexture diffuseProbeGBuffer2;
    }
}