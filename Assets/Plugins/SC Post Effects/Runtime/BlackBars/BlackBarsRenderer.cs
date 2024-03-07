using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class BlackBarsRenderer : ScriptableRendererFeature
    {
        class BlackBarsRenderPass : PostEffectRenderer<BlackBars>
        {
            public BlackBarsRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.BlackBars;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<BlackBars>();
                
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

                Material.SetVector("_Size", new Vector2(volumeSettings.size.value / 10f, volumeSettings.maxSize.value * 5));

                FinalBlit(this, context, cmd, renderingData, (int)volumeSettings.mode.value);
            }
        }

        BlackBarsRenderPass m_ScriptablePass;
        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings(false);
        
        public override void Create()
        {
            m_ScriptablePass = new BlackBarsRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}