using UnityEngine.Rendering;

namespace AdvancedRenderPipeline.Runtime.PipelineProcessors {
    public class GeneralPipelineProcessor : PipelineProcessor {

        public GeneralPipelineProcessor() {
            _processorDesc = "Process General Pipeline";
        }

        public override void Process(ScriptableRenderContext context) {
            _context = context;
            _cmd = CommandBufferPool.Get(_processorDesc);
            
            SetupSkybox();

            FirstFrameSetup();
            
            DisposeCommandBuffer();
        }

        public void FirstFrameSetup() {
            var settings = AdvancedRenderPipeline.settings;

            if (!AdvancedRenderPipeline.instance.IsOnFirstFrame) return;
            
            _cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_16, settings.blueNoise16);
            _cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_64, settings.blueNoise64);
            _cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_256, settings.blueNoise256);
            _cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_512, settings.blueNoise512);
            _cmd.SetGlobalTexture(ShaderKeywordManager.BLUE_NOISE_1024, settings.blueNoise1024);
            _cmd.SetGlobalTexture(ShaderKeywordManager.PREINTEGRATED_DGF_LUT, settings.iblLut);
            _cmd.SetGlobalTexture(ShaderKeywordManager.PREINTEGRATED_D_LUT, settings.diffuseIBLLut);
            _cmd.SetGlobalTexture(ShaderKeywordManager.PREINTEGRATED_GF_LUT, settings.specularIBLLut);
            _cmd.SetGlobalTexture(ShaderKeywordManager.GLOBAL_ENV_MAP_SPECULAR, settings.globalEnvMapSpecular);
            _cmd.SetGlobalTexture(ShaderKeywordManager.GLOBAL_ENV_MAP_DIFFUSE, settings.globalEnvMapDiffuse);
                
            ExecuteCommand();
        }
        
        internal void SetupSkybox() {
            var settings = AdvancedRenderPipeline.settings;
            _cmd.SetGlobalFloat(ShaderKeywordManager.GLOBAL_ENV_MAP_ROTATION, settings.globalEnvMapRotation);
            _cmd.SetGlobalFloat(ShaderKeywordManager.SKYBOX_MIP_LEVEL, settings.skyboxMipLevel);
            _cmd.SetGlobalFloat(ShaderKeywordManager.SKYBOX_INTENSITY, settings.skyboxIntensity);
        }

        public override void Dispose() {
            DisposeCommandBuffer();
        }
    }
}