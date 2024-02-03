using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SCPE
{
    public class AmbientOcclusion2DRenderer : ScriptableRendererFeature
    {
        class AmbientOcclusion2DRenderPass : PostEffectRenderer<AmbientOcclusion2D>
        {
            private int aoTexID = Shader.PropertyToID("_AO");

            private RTHandle ao;
            private RTHandle blurBuffer1;
            private RTHandle blurBuffer2;
            
            public AmbientOcclusion2DRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.AO2D;
                ProfilerTag = GetProfilerTag();
            }
            
            enum Pass
            {
                LuminanceDiff,
                Blur,
                Blend,
                Debug
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion2D>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);
                
                ao = GetTemporaryRT(ref ao, cameraTextureDescriptor, GraphicsFormat.R8_UNorm, FilterMode.Bilinear, "ao", volumeSettings.downscaling.value);
                
                blurBuffer1 = GetTemporaryRT(ref blurBuffer1, cameraTextureDescriptor, GraphicsFormat.R8_UNorm, FilterMode.Bilinear, "BlurBuffer1", volumeSettings.downscaling.value);
                blurBuffer2 = GetTemporaryRT(ref blurBuffer2, cameraTextureDescriptor, GraphicsFormat.R8_UNorm, FilterMode.Bilinear, "BlurBuffer2", volumeSettings.downscaling.value);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);
                
                cmd.SetGlobalFloat("_SampleDistance", volumeSettings.distance.value);
                float luminanceThreshold = QualitySettings.activeColorSpace == ColorSpace.Gamma ? Mathf.GammaToLinearSpace(volumeSettings.luminanceThreshold.value) : volumeSettings.luminanceThreshold.value;

                cmd.SetGlobalFloat("_Threshold", luminanceThreshold);
                cmd.SetGlobalFloat("_Blur", volumeSettings.blurAmount.value);
                cmd.SetGlobalFloat("_Intensity", volumeSettings.intensity.value);
                
                Blit(cmd, cameraColorTarget, ao, base.Material, (int)Pass.LuminanceDiff);

                //Pass AO into blur target texture
                BlitCopy(cmd,ao, blurBuffer1);
                
                for (int i = 0; i < volumeSettings.iterations.value; i++)
                {
                    // horizontal blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4((volumeSettings.blurAmount.value) / renderingData.cameraData.camera.scaledPixelWidth, 0, 0, 0));
                    Blit(this, cmd, blurBuffer1, blurBuffer2, Material, (int)Pass.Blur);

                    // vertical blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(0, (volumeSettings.blurAmount.value) / renderingData.cameraData.camera.scaledPixelHeight, 0, 0));
                    Blit(this, cmd, blurBuffer2, blurBuffer1, Material, (int)Pass.Blur);
                }
                
                cmd.SetGlobalTexture(aoTexID, blurBuffer1);

                FinalBlit(this, context, cmd, renderingData, (volumeSettings.aoOnly.value) ? (int)Pass.Debug : (int)Pass.Blend);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                base.OnCameraCleanup(cmd);

                if (ShouldReleaseRT())
                {
                    ReleaseRT(ao);
                    ReleaseRT(blurBuffer1);
                    ReleaseRT(blurBuffer2);
                }
            }
        }

        AmbientOcclusion2DRenderPass m_ScriptablePass;
        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings(true);
        
        public override void Create()
        {
            m_ScriptablePass = new AmbientOcclusion2DRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}