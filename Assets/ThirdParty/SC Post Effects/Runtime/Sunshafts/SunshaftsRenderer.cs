using System;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCPE
{
    public class SunshaftsRenderer : ScriptableRendererFeature
    {
        class SunshaftsRenderPass : PostEffectRenderer<Sunshafts>
        {
            private int skyboxBufferID = Shader.PropertyToID("_SunshaftBuffer");

            private RTHandle blurBuffer1;
            private RTHandle blurBuffer2;

            public SunshaftsRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Sunshafts;
                requiresDepth = true;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Sunshafts>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                base.OnCameraSetup(cmd, ref renderingData);
                
                int res = (int)volumeSettings.resolution.value;

                RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                
                blurBuffer1 = GetTemporaryRT(ref blurBuffer1, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "SunshaftsBlurBuffer1", res);
                blurBuffer2 = GetTemporaryRT(ref blurBuffer2, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Bilinear, "SunshaftsBlurBuffer2", res);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);

               
            }

            public enum Pass
            {
                SkySource,
                RadialBlur,
                Blend
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                
                var cmd = GetCommandBuffer(ref renderingData);
                
                CopyTargets(cmd, renderingData);

                #region Parameters
                float sunIntensity = (volumeSettings.useCasterIntensity.value && RenderSettings.sun) ? RenderSettings.sun.intensity : volumeSettings.sunShaftIntensity.value;
                
                cmd.SetGlobalVector("_SunPosition",-RenderSettings.sun.transform.forward * 1E10f);
                cmd.SetGlobalFloat("_BlendMode", (int)volumeSettings.blendMode.value);
                cmd.SetGlobalColor("_SunColor", (volumeSettings.useCasterColor.value && RenderSettings.sun) ? RenderSettings.sun.color : volumeSettings.sunColor.value);
                cmd.SetGlobalColor("_SunThreshold", volumeSettings.sunThreshold.value);
                
                cmd.SetGlobalVector(ShaderParameters.Params, new Vector4(sunIntensity, volumeSettings.falloff.value, 0, 0));
                #endregion

                SetViewProjectionMatrixUniforms(cmd, renderingData.cameraData);
                
                Blit(this, cmd, cameraColorTarget, blurBuffer1, Material, (int)Pass.SkySource);
                
                #region Blur
                cmd.BeginSample("Sunshafts blur");
                
                float offset = volumeSettings.length.value * (1.0f / 768.0f);

                int iterations = (volumeSettings.highQuality.value) ? 2 : 1;
                float blurAmount = (volumeSettings.highQuality.value) ? volumeSettings.length.value / 2.5f : volumeSettings.length.value;

                for (int i = 0; i < iterations; i++)
                {
                    Blit(this, cmd, blurBuffer1, blurBuffer2, Material, (int)Pass.RadialBlur);
                    offset = blurAmount * (((i * 2.0f + 1.0f) * 6.0f)) / renderingData.cameraData.camera.pixelWidth;
                    cmd.SetGlobalFloat(ShaderParameters.BlurRadius, offset);

                    Blit(this, cmd, blurBuffer2, blurBuffer1, Material, (int)Pass.RadialBlur);
                    offset = blurAmount * (((i * 2.0f + 1.0f) * 6.0f)) / renderingData.cameraData.camera.pixelHeight;
                    cmd.SetGlobalFloat(ShaderParameters.BlurRadius, offset);

                }
                cmd.EndSample("Sunshafts blur");

                #endregion
                
                cmd.SetGlobalTexture(skyboxBufferID, blurBuffer1);

                FinalBlit(this, context, cmd, renderingData, (int)Pass.Blend);
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

        SunshaftsRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new SunshaftsRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}
