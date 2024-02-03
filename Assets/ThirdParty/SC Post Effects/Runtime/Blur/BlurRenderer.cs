using System;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class BlurRenderer : ScriptableRendererFeature
    {
        class BlurRenderPass : PostEffectRenderer<Blur>
        {
            private RTHandle blurBuffer1;
            private RTHandle blurBuffer2;

            enum Pass
            {
                Blend,
                BlendDepthFade,
                Gaussian,
                Box
            }
            public BlurRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Blur;
                ProfilerTag = GetProfilerTag();
            }
            
            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Blur>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);
                
                blurBuffer1 = GetTemporaryRT(ref blurBuffer1, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "BlurBuffer1", volumeSettings.downscaling.value);
                blurBuffer2 = GetTemporaryRT(ref blurBuffer2, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "BlurBuffer2", volumeSettings.downscaling.value);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);
                BlitCopy(cmd, cameraColorTarget, blurBuffer1);

                int blurPass = (volumeSettings.mode == Blur.BlurMethod.Gaussian) ? (int)Pass.Gaussian : (int)Pass.Box;

                for (int i = 0; i < volumeSettings.iterations.value; i++)
                {
                    //Safeguard for exploding GPUs
                    if (volumeSettings.iterations.value > 12) return;

                    // horizontal blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(volumeSettings.amount.value / renderingData.cameraData.camera.scaledPixelWidth, 0, 0, 0));
                    Blit(this, cmd, blurBuffer1, blurBuffer2, Material, blurPass);

                    // vertical blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(0, volumeSettings.amount.value / renderingData.cameraData.camera.scaledPixelHeight, 0, 0));
                    Blit(this, cmd, blurBuffer2, blurBuffer1, Material, blurPass);

                    //Double blur
                    if (volumeSettings.highQuality.value)
                    {
                        // horizontal blur
                        cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(volumeSettings.amount.value / renderingData.cameraData.camera.scaledPixelWidth, 0, 0, 0));
                        Blit(this, cmd, blurBuffer1, blurBuffer2, Material, blurPass);

                        // vertical blur
                        cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(0, volumeSettings.amount.value / renderingData.cameraData.camera.scaledPixelHeight, 0, 0));
                        Blit(this, cmd, blurBuffer2, blurBuffer1, Material, blurPass);
                    }
                }
                
                cmd.SetGlobalTexture("_BlurredTex", blurBuffer1);
                
                if(volumeSettings.distanceFade.value) cmd.SetGlobalVector(ShaderParameters.FadeParams, new Vector4(volumeSettings.startFadeDistance.value, volumeSettings.endFadeDistance.value, 0, volumeSettings.distanceFade.value ? 1 : 0));

                FinalBlit(this, context, cmd, renderingData, volumeSettings.distanceFade.value ? (int)Pass.BlendDepthFade : (int)Pass.Blend);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                base.OnCameraCleanup(cmd);

                if (ShouldReleaseRT())
                {
                    ReleaseRT(blurBuffer1);
                    ReleaseRT(blurBuffer2);
                }
            }

        }

        BlurRenderPass m_ScriptablePass;
        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings(false);
        
        public override void Create()
        {
            m_ScriptablePass = new BlurRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}