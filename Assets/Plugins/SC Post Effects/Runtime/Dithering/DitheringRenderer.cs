using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class DitheringRenderer : ScriptableRendererFeature
    {
        class DitheringRenderPass : PostEffectRenderer<Dithering>
        {
            public DitheringRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Dithering;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Dithering>();
                
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

                var lutTexture = volumeSettings.lut.value == null ? Texture2D.blackTexture : volumeSettings.lut.value;
                Material.SetTexture("_LUT", lutTexture);
                float luminanceThreshold = QualitySettings.activeColorSpace == ColorSpace.Gamma ? Mathf.LinearToGammaSpace(volumeSettings.luminanceThreshold.value) : volumeSettings.luminanceThreshold.value;

                Vector4 ditherParams = new Vector4(0f, volumeSettings.tiling.value, luminanceThreshold, volumeSettings.intensity.value);
                Material.SetVector("_Dithering_Coords", ditherParams);

                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        DitheringRenderPass m_ScriptablePass;
        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();
        
        public override void Create()
        {
            m_ScriptablePass = new DitheringRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}