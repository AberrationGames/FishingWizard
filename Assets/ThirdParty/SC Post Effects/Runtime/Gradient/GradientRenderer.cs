using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class GradientRenderer : ScriptableRendererFeature
    {
        class GradientRenderPass : PostEffectRenderer<Gradient>
        {
            public GradientRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Gradient;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Gradient>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                
                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);

                if (volumeSettings.gradientTex.value) Material.SetTexture("_Gradient", volumeSettings.gradientTex.value);
                Material.SetColor("_Color1", volumeSettings.color1.value);
                Material.SetColor("_Color2", volumeSettings.color2.value);
                Material.SetFloat("_Rotation", volumeSettings.rotation.value * 360f);
                Material.SetFloat("_Intensity", volumeSettings.intensity.value);
                Material.SetFloat("_BlendMode", (int)volumeSettings.mode.value);

                FinalBlit(this, context, cmd, renderingData, (int)volumeSettings.input.value);
            }
        }

        GradientRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new GradientRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}