using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class LightStreaksRenderer : ScriptableRendererFeature
    {
        class LightStreaksRenderPass : PostEffectRenderer<LightStreaks>
        {
            private readonly int emissionTexID = Shader.PropertyToID("_BloomTex");
            private RTHandle emissionTex;
            private RTHandle blurBuffer1;
            private RTHandle blurBuffer2;

            enum Pass
            {
                LuminanceDiff,
                BlurFast,
                Blur,
                Blend,
                Debug
            }

            public LightStreaksRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.LightStreaks;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<LightStreaks>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);
                
                emissionTex = GetTemporaryRT(ref emissionTex, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "LightStreaks");
                
                blurBuffer1 = GetTemporaryRT(ref blurBuffer1, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "LightStreaksBlurBuffer1", volumeSettings.downscaling.value);
                blurBuffer2 = GetTemporaryRT(ref blurBuffer2, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "LightStreaksBlurBuffer2", volumeSettings.downscaling.value);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);

                int blurMode = (volumeSettings.quality.value == LightStreaks.Quality.Performance) ? (int)Pass.BlurFast : (int)Pass.Blur;

                float luminanceThreshold = Mathf.GammaToLinearSpace(volumeSettings.luminanceThreshold.value);

                Material.SetVector(ShaderParameters.Params, new Vector4(luminanceThreshold, volumeSettings.intensity.value, 0f, 0f));

                CopyTargets(cmd, renderingData);
                
                Blit(this, cmd, cameraColorTarget, emissionTex, Material, (int)Pass.LuminanceDiff);
                BlitCopy(cmd, emissionTex, blurBuffer1);

                float ratio = Mathf.Clamp(volumeSettings.direction.value, -1, 1);
                float rw = ratio < 0 ? -ratio * 1f : 0f;
                float rh = ratio > 0 ? ratio * 4f : 0f;

                int iterations = (volumeSettings.quality.value == LightStreaks.Quality.Performance) ? volumeSettings.iterations.value * 3 : volumeSettings.iterations.value;

                for (int i = 0; i < iterations; i++)
                {
                    // horizontal blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(rw * volumeSettings.blur.value / renderingData.cameraData.camera.scaledPixelWidth, rh / renderingData.cameraData.camera.scaledPixelHeight, 0, 0));
                    Blit(this, cmd, blurBuffer1, blurBuffer2, Material, blurMode);

                    // vertical blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4((rw * volumeSettings.blur.value) * 2f / renderingData.cameraData.camera.scaledPixelWidth, rh * 2f / renderingData.cameraData.camera.scaledPixelHeight, 0, 0));
                    Blit(this, cmd, blurBuffer2, blurBuffer1, Material, blurMode);
                }

                cmd.SetGlobalTexture(emissionTexID, blurBuffer1);

                FinalBlit(this, context, cmd, renderingData, (volumeSettings.debug.value) ? (int)Pass.Debug : (int)Pass.Blend);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                base.OnCameraCleanup(cmd);

                if (ShouldReleaseRT())
                {
                    ReleaseRT(emissionTex);
                    ReleaseRT(blurBuffer1);
                    ReleaseRT(blurBuffer2);
                }
            }
        }

        LightStreaksRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new LightStreaksRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}