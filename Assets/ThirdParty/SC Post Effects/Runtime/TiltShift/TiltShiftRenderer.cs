using System;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class TiltShiftRenderer : ScriptableRendererFeature
    {
        class TiltShiftRenderPass : PostEffectRenderer<TiltShift>
        {
            public TiltShiftRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.GetInjectionPoint();
                shaderName = ShaderNames.TiltShift;
                ProfilerTag = GetProfilerTag();
            }

            enum Pass
            {
                FragHorizontal,
                FragHorizontalHQ,
                FragRadial,
                FragRadialHQ,
                FragDebug
            }

            public override void Setup(ScriptableRenderer renderer, RenderingData renderingData)
            {
                volumeSettings = VolumeManager.instance.stack.GetComponent<TiltShift>();
                
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

                Material.SetVector(ShaderParameters.Params, new Vector4(volumeSettings.areaSize.value, volumeSettings.areaFalloff.value, volumeSettings.amount.value, (int)volumeSettings.mode.value));
                Material.SetFloat("_Offset", volumeSettings.offset.value);
                Material.SetFloat("_Angle", volumeSettings.angle.value);
                
                int pass = (int)volumeSettings.mode.value + (int)volumeSettings.quality.value;
                switch ((int)volumeSettings.mode.value)
                {
                    case 0:
                        pass = 0 + (int)volumeSettings.quality.value;
                        break;
                    case 1:
                        pass = 2 + (int)volumeSettings.quality.value;
                        break;
                }

                FinalBlit(this, context, cmd, renderingData, TiltShift.debug ? (int)Pass.FragDebug : pass);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                base.OnCameraCleanup(cmd);
            }
        }

        TiltShiftRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new TiltShiftRenderPass(settings);
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