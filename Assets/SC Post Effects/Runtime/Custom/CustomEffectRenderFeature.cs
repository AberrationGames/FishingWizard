using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SCPE
{
    public class CustomEffectRenderFeature : ScriptableRendererFeature
    {
        public class CustomEffectPass : PostEffectRenderer<VolumeComponent>
        {
            public CustomEffectPass(PostEffectSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                //Fake, but ensure error free operation
                shaderName = "Legacy Shaders/Diffuse";
                ProfilerTag = "Custom Post Processing";
                
                //Assign the custom material
                Material = settings.material;
            }
            
            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                if (!ShouldRenderForCamera(renderingData) || !Material) return;
                
                this.cameraColorTarget = GetCameraTarget(renderer);
                
                renderer.EnqueuePass(this);
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = GetCommandBuffer(ref renderingData);
                
                CopyTargets(cmd, renderingData);
                
                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        [Serializable]
        public class PostEffectSettings : EffectBaseSettings
        {
            [Space]
            public Material material;
            [Tooltip("Executes the effect before transparent materials are rendered.")]
            public bool skipTransparents;

        }

        CustomEffectPass m_ScriptablePass;
        [SerializeField]
        public PostEffectSettings settings = new PostEffectSettings();
        
        public override void Create()
        {
            m_ScriptablePass = new CustomEffectPass(settings);
            m_ScriptablePass.renderPassEvent = settings.skipTransparents ? RenderPassEvent.BeforeRenderingTransparents : settings.GetInjectionPoint();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer, renderingData);
        }
    }
}