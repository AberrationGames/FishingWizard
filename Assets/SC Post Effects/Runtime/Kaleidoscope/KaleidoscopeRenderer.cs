using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class KaleidoscopeRenderer : ScriptableRendererFeature
    {
        class KaleidoscopeRenderPass : PostEffectRenderer<Kaleidoscope>
        {
            public KaleidoscopeRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Kaleidoscope;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Kaleidoscope>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);
            }

            private static readonly int _KaleidoscopeSplits = Shader.PropertyToID("_KaleidoscopeSplits");

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                
                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);

                Material.SetVector(ShaderParameters.Params, new Vector4(Mathf.PI * 2 / Mathf.Max(1, volumeSettings.radialSplits.value), volumeSettings.maintainAspectRatio.value ? 1 : 0, volumeSettings.center.value.x, volumeSettings.center.value.y));
                Material.SetVector(_KaleidoscopeSplits, new Vector4(volumeSettings.horizontalSplits.value, volumeSettings.verticalSplits.value, 0, 0));

                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        KaleidoscopeRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings(false);

        public override void Create()
        {
            m_ScriptablePass = new KaleidoscopeRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}