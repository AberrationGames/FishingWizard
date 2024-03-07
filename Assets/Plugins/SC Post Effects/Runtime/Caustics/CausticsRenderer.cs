using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class CausticsRenderer : ScriptableRendererFeature
    {
        class CausticsRenderPass : PostEffectRenderer<Caustics>
        {
            public CausticsRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Caustics;
                this.requiresDepth = true;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Caustics>();
                
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

                if(volumeSettings.causticsTexture.value) Material.SetTexture("_CausticsTex", volumeSettings.causticsTexture.value);
                Material.SetFloat("_LuminanceThreshold", Mathf.GammaToLinearSpace(volumeSettings.luminanceThreshold.value));

                if (volumeSettings.projectFromSun.value) SetMainLightProjection(cmd, renderingData);

                Material.SetVector("_CausticsParams", new Vector4(volumeSettings.size.value, volumeSettings.speed.value, volumeSettings.projectFromSun.value ? 1 : 0,  volumeSettings.brightness.value * volumeSettings.intensity.value));
                Material.SetVector("_HeightParams", new Vector4(volumeSettings.minHeight.value, volumeSettings.minHeightFalloff.value, volumeSettings.maxHeight.value, volumeSettings.maxHeightFalloff.value));
            
                cmd.SetGlobalVector("_FadeParams", new Vector4(volumeSettings.startFadeDistance.value, volumeSettings.endFadeDistance.value, 0, volumeSettings.distanceFade.value ? 1 : 0));
                
                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        CausticsRenderPass m_ScriptablePass;
        [System.Serializable]
        public class Causticsettings : EffectBaseSettings
        {
            [Header("Effect specific")]
            [Tooltip("Executes the effect before transparent materials are rendered.")]
            public bool skipTransparents;
        }

        [SerializeField]
        public Causticsettings settings = new Causticsettings();
        
        public override void Create()
        {
            m_ScriptablePass = new CausticsRenderPass(settings);
            m_ScriptablePass.renderPassEvent = settings.skipTransparents ? RenderPassEvent.BeforeRenderingTransparents : settings.GetInjectionPoint();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}