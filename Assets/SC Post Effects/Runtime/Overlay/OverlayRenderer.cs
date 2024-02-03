using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class OverlayRenderer : ScriptableRendererFeature
    {
        class OverlayRenderPass : PostEffectRenderer<Overlay>
        {
            public OverlayRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Overlay;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Overlay>();
                
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

                if (volumeSettings.overlayTex.value) Material.SetTexture("_OverlayTex", volumeSettings.overlayTex.value);
                Material.SetVector("_Params", new Vector4(volumeSettings.intensity.value, Mathf.Pow(volumeSettings.tiling.value + 1, 2), volumeSettings.autoAspect.value ? 1f : 0f, (int)volumeSettings.blendMode.value));

                float luminanceThreshold = QualitySettings.activeColorSpace == ColorSpace.Gamma ? Mathf.LinearToGammaSpace(volumeSettings.luminanceThreshold.value) : volumeSettings.luminanceThreshold.value;
                Material.SetFloat("_LuminanceThreshold", luminanceThreshold);
                
                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        OverlayRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new OverlayRenderPass(settings);
                }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}