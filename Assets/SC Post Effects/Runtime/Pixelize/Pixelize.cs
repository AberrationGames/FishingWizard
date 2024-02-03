using UnityEngine.Rendering.Universal;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCPE
{
    [Serializable, VolumeComponentMenu("SC Post Effects/Retro/Pixelize")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class Pixelize : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter amount = new ClampedFloatParameter(0f, 0f, 1f);
        public IntParameter resolution = new IntParameter(240);

        public enum Resolution
        {
            Custom = 1,
            [InspectorName("600p")]
            Sixhundred = 600,
            [InspectorName("480p")]
            FourEighty = 480,
            [InspectorName("240p")]
            TwoFourty = 240,
            [InspectorName("200p")]
            TwoHundred = 200,
            [InspectorName("160p")]
            HundredSixty = 160
        }

        [Serializable]
        public sealed class ResolutionPreset : VolumeParameter<Resolution> { }

        public ResolutionPreset resolutionPreset = new ResolutionPreset { value = Resolution.Custom };

        [Tooltip("When disabled, pixels will retain a square aspect ratio")]
        public BoolParameter preserveAspectRatio = new BoolParameter(false);
        [Tooltip("When enabled, pixels are shifted by half. Mostly has a visible effect on extremely low resolutions")]
        public BoolParameter centerPixel = new BoolParameter(true);
        
        public bool IsActive() => amount.value > 0f && this.active;

        public bool IsTileCompatible() => false;
        
        [SerializeField]
        public Shader shader;

        private void Reset()
        {
            SerializeShader();
        }

        private bool SerializeShader()
        {
            bool wasSerialized = !shader;
            shader = Shader.Find(ShaderNames.Pixelize);

            return wasSerialized;
        }
    }
}