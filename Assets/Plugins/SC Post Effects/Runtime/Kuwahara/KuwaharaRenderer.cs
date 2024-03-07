using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class KuwaharaRenderer : ScriptableRendererFeature
    {
        class KuwaharaRenderPass : PostEffectRenderer<Kuwahara>
        {
            private int mode;
            
            public KuwaharaRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.Kuwahara;
                ProfilerTag = GetProfilerTag();
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<Kuwahara>();
                
                base.Setup(renderer, renderingData);
                
                if (!render || !volumeSettings.IsActive()) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                requiresDepth = volumeSettings.mode == Kuwahara.KuwaharaMode.DepthFade;

                base.ConfigurePass(cmd, cameraTextureDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);
                
                mode = (int)volumeSettings.mode.value;
                if (renderingData.cameraData.camera.orthographic) mode = (int)Kuwahara.KuwaharaMode.FullScreen;

                CopyTargets(cmd, renderingData);

                Material.SetFloat("_Radius", (float)volumeSettings.radius);
                if(mode == (int)Kuwahara.KuwaharaMode.DepthFade) Material.SetVector("_FadeParams", new Vector4(volumeSettings.startFadeDistance.value, volumeSettings.endFadeDistance.value, 0, 0));

                FinalBlit(this, context, cmd, renderingData, mode);
            }
        }

        KuwaharaRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new KuwaharaRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}