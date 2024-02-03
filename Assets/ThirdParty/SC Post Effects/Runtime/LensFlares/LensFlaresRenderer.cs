using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class LensFlaresRenderer : ScriptableRendererFeature
    {
        class LensFlaresRenderPass : PostEffectRenderer<LensFlares>
        {
            private int flaresTexID;
            private int emissionTexID;
            
            private RTHandle emissionTex;
            private RTHandle flaresTex;
            private RTHandle blurBuffer1;
            private RTHandle blurBuffer2;
            
            public LensFlaresRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.LensFlares;
                ProfilerTag = GetProfilerTag();
                
                emissionTexID = Shader.PropertyToID("_BloomTex");
                flaresTexID = Shader.PropertyToID("_FlaresTex");
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<LensFlares>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);
                
                emissionTex = GetTemporaryRT(ref emissionTex, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "emissionTex", 2);
                cmd.SetGlobalTexture(emissionTexID, emissionTex);
                
                flaresTex = GetTemporaryRT(ref flaresTex, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "flaresTex", 2);

                cmd.SetGlobalTexture(flaresTexID, flaresTex);
                
                blurBuffer1 = GetTemporaryRT(ref blurBuffer1, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "LensFlareBlurBuffer1", 2);
                blurBuffer2 = GetTemporaryRT(ref blurBuffer2, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "LensFlareBlurBuffer2", 2);
            }
            
            enum Pass
            {
                LuminanceDiff,
                Ghosting,
                Blur,
                Blend,
                Debug
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);

                Material.SetFloat("_Intensity", volumeSettings.intensity.value);
                float luminanceThreshold = Mathf.GammaToLinearSpace(volumeSettings.luminanceThreshold.value);
                Material.SetFloat("_Threshold", luminanceThreshold);
                Material.SetFloat("_Distance", volumeSettings.distance.value);
                Material.SetFloat("_Falloff", volumeSettings.falloff.value);
                Material.SetFloat("_Ghosts", volumeSettings.iterations.value);
                Material.SetFloat("_HaloSize", volumeSettings.haloSize.value);
                Material.SetFloat("_HaloWidth", volumeSettings.haloWidth.value);
                Material.SetFloat("_ChromaticAbberation", volumeSettings.chromaticAbberation.value);

                Material.SetTexture("_ColorTex", volumeSettings.colorTex.value ? volumeSettings.colorTex.value : Texture2D.whiteTexture as Texture);
                Material.SetTexture("_MaskTex", volumeSettings.maskTex.value ? volumeSettings.maskTex.value : Texture2D.whiteTexture as Texture);
                
                Blit(this, cmd, cameraColorTarget, emissionTex, Material, (int)Pass.LuminanceDiff);
                Blit(this, cmd, emissionTex, flaresTex, Material, (int)Pass.Ghosting );
                
                // downsample screen copy into smaller RT, release screen RT
                BlitCopy(cmd,flaresTex, blurBuffer1);
                for (int i = 0; i < volumeSettings.passes.value; i++)
                {
                    // horizontal blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(volumeSettings.blur.value / renderingData.cameraData.camera.scaledPixelWidth, 0, 0, 0));
                    Blit(this, cmd, blurBuffer1, blurBuffer2, Material, (int)Pass.Blur );

                    // vertical blur
                    cmd.SetGlobalVector(ShaderParameters.BlurOffsets, new Vector4(0, volumeSettings.blur.value / renderingData.cameraData.camera.scaledPixelHeight, 0, 0));
                    Blit(this, cmd, blurBuffer2, blurBuffer1, Material, (int)Pass.Blur );

                }

                cmd.SetGlobalTexture(flaresTexID, blurBuffer1);
                
                FinalBlit(this, context, cmd, renderingData, (volumeSettings.debug.value) ? (int)Pass.Debug : (int)Pass.Blend);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                base.OnCameraCleanup(cmd);

                if (ShouldReleaseRT())
                {
                    ReleaseRT(emissionTex);
                    ReleaseRT(flaresTex);
                    ReleaseRT(blurBuffer1);
                    ReleaseRT(blurBuffer2);
                }
            }
        }

        LensFlaresRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new LensFlaresRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}