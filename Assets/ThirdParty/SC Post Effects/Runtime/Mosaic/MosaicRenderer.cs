using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class MosaicRenderer : ScriptableRendererFeature
    {
        class MosaicRenderPass : PostEffectRenderer<Mosaic>
        {
            public MosaicRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Mosaic;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Mosaic>();
                
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

                float size = volumeSettings.size.value;

                switch ((Mosaic.MosaicMode)volumeSettings.mode)
                {
                    case Mosaic.MosaicMode.Triangles:
                        size = 10f / volumeSettings.size.value;
                        break;
                    case Mosaic.MosaicMode.Hexagons:
                        size = volumeSettings.size.value / 10f;
                        break;
                    case Mosaic.MosaicMode.Circles:
                        size = (1 - volumeSettings.size.value) * 100f;
                        break;
                }

                Vector4 parameters = new Vector4(size, ((renderingData.cameraData.camera.scaledPixelWidth * 2 / renderingData.cameraData.camera.scaledPixelHeight) * size / Mathf.Sqrt(3f)), 0f, 0f);

                Material.SetVector("_Params", parameters);

                FinalBlit(this, context, cmd, renderingData, (int)volumeSettings.mode.value);
            }
        }

        MosaicRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new MosaicRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}