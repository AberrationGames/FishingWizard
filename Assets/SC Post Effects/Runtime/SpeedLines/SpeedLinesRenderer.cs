using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class SpeedLinesRenderer : ScriptableRendererFeature
    {
        class SpeedLinesRenderPass : PostEffectRenderer<SpeedLines>
        {
            public SpeedLinesRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.SpeedLines;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<SpeedLines>();
                
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

                float falloff = 2f + (volumeSettings.falloff.value - 0.0f) * (16.0f - 2f) / (1.0f - 0.0f);
                Material.SetVector("_Params", new Vector4(volumeSettings.intensity.value, falloff, volumeSettings.size.value * 2, 0));
                if (volumeSettings.noiseTex.value) Material.SetTexture("_NoiseTex", volumeSettings.noiseTex.value);

                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        SpeedLinesRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new SpeedLinesRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}