using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class PixelizeRenderer : ScriptableRendererFeature
    {
        class PixelizeRenderPass : PostEffectRenderer<Pixelize>
        {
            public PixelizeRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Pixelize;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Pixelize>();
                
                base.Setup(renderer, renderingData);

                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                base.ConfigurePass(cmd, cameraTextureDescriptor);
            }
            
            private static readonly int _PixelizeParams = Shader.PropertyToID("_PixelizeParams");
            private static Vector4 pixelizeParams;

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);
                
                var resolution = volumeSettings.resolutionPreset.value == Pixelize.Resolution.Custom ? volumeSettings.resolution.value : (int)volumeSettings.resolutionPreset.value;

                pixelizeParams.x = (volumeSettings.preserveAspectRatio.value ? renderingData.cameraData.camera.scaledPixelWidth : renderingData.cameraData.camera.scaledPixelHeight) / (float)resolution;
                pixelizeParams.y = renderingData.cameraData.camera.scaledPixelHeight / (float)resolution;
                pixelizeParams.z = volumeSettings.amount.value;
                pixelizeParams.w = volumeSettings.centerPixel.value ? 1 : 0;
                
                Material.SetVector(_PixelizeParams, pixelizeParams);

                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        PixelizeRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new PixelizeRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
        
        public void OnDestroy()
        {
            m_ScriptablePass.Dispose();
        }
    }
}