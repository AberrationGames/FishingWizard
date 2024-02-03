using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class LUTRenderer : ScriptableRendererFeature
    {
        class LUTRenderPass : PostEffectRenderer<LUT>
        {
            public LUTRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.LUT;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<LUT>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                requiresDepth = volumeSettings.mode == LUT.Mode.DistanceBased;

                base.ConfigurePass(cmd, cameraTextureDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (LUT.Bypass) return;
                
                if (!render || !volumeSettings.IsActive()) return;
                
                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);

                Material.SetVector("_LUT_Params", new Vector4(volumeSettings.lutNear.value ? volumeSettings.intensity.value : 0f, volumeSettings.invert.value));
                
                if (volumeSettings.lutNear.value)
                {
                    Material.SetTexture("_LUT_Near", volumeSettings.lutNear.value);
                }

                if ((int)volumeSettings.mode.value == 1)
                {
                    Material.SetVector("_FadeParams", new Vector4(volumeSettings.startFadeDistance.value, volumeSettings.endFadeDistance.value, 0, 0));

                    if (volumeSettings.lutFar.value) Material.SetTexture("_LUT_Far", volumeSettings.lutFar.value);
                }
                
                Material.SetVector(ShaderParameters.Params, new Vector4(volumeSettings.vibranceRGBBalance.value.r, volumeSettings.vibranceRGBBalance.value.g, volumeSettings.vibranceRGBBalance.value.b, volumeSettings.vibrance.value));
                
                FinalBlit(this, context, cmd, renderingData, (int)volumeSettings.mode.value);
            }
        }

        LUTRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new LUTRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}
