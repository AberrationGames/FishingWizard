using UnityEngine.Rendering.Universal;

using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class SketchRenderer : ScriptableRendererFeature
    {
        class SketchRenderPass : PostEffectRenderer<Sketch>
        {
            public SketchRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Sketch;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Sketch>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                requiresDepth = volumeSettings.projectionMode == Sketch.SketchProjectionMode.WorldSpace;

                base.ConfigurePass(cmd, cameraTextureDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);
                
                CopyTargets(cmd, renderingData);

                if (volumeSettings.strokeTex.value) Material.SetTexture("_Strokes", volumeSettings.strokeTex.value);

                Material.SetVector("_Params", new Vector4(0, (int)volumeSettings.blendMode.value, volumeSettings.intensity.value, ((int)volumeSettings.projectionMode.value == 1) ? volumeSettings.tiling.value * 0.1f : volumeSettings.tiling.value));
                Material.SetVector("_Brightness", volumeSettings.brightness.value);

                FinalBlit(this, context, cmd, renderingData, (int)volumeSettings.projectionMode.value);
            }
        }

        SketchRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new SketchRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}
