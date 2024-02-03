using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class CloudShadowsRenderer : ScriptableRendererFeature
    {
        class CloudShadowsRenderPass : PostEffectRenderer<CloudShadows>
        {
            public CloudShadowsRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                this.shaderName = ShaderNames.CloudShadows;
                this.requiresDepth = true;
                ProfilerTag = GetProfilerTag();
            }
            
            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<CloudShadows>();
                
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
                
                var noiseTexture = volumeSettings.texture.value == null ? Texture2D.whiteTexture : volumeSettings.texture.value;
                Material.SetTexture("_NoiseTex", noiseTexture);

                float cloudsSpeed = volumeSettings.speed.value * 0.1f;
                Material.SetVector("_CloudParams", new Vector4(volumeSettings.size.value * 0.01f, volumeSettings.direction.value.x * cloudsSpeed, volumeSettings.direction.value.y * cloudsSpeed, volumeSettings.density.value));             

                if (volumeSettings.projectFromSun.value) SetMainLightProjection(cmd, renderingData);
                Material.SetFloat("_ProjectionEnabled", volumeSettings.projectFromSun.value ? 1 : 0);

                cmd.SetGlobalVector("_FadeParams", new Vector4(volumeSettings.startFadeDistance.value, volumeSettings.endFadeDistance.value, 0, 0));

                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        CloudShadowsRenderPass m_ScriptablePass;
        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();
        
        public override void Create()
        {
            m_ScriptablePass = new CloudShadowsRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}