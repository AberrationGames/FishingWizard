using UnityEngine.Rendering.Universal;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCPE
{
    [Serializable, VolumeComponentMenu("SC Post Effects/Blurring/Tilt Shift")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class TiltShift : VolumeComponent, IPostProcessComponent
    {
        public enum TiltShiftMethod
        {
            Horizontal,
            Radial,
        }

        [Serializable]
        public sealed class TiltShifMethodParameter : VolumeParameter<TiltShiftMethod> { }

        [Tooltip("The amount of blurring that must be performed")]
        public ClampedFloatParameter amount = new ClampedFloatParameter(0f, 0f, 1f);
        
        public TiltShifMethodParameter mode = new TiltShifMethodParameter();

        public enum Quality
        {
            Performance,
            Appearance
        }

        [Serializable]
        public sealed class TiltShiftQualityParameter : VolumeParameter<Quality> { }

        [Tooltip("Choose to use more texture samples, for a smoother blur when using a high blur amout")]
        public TiltShiftQualityParameter quality = new TiltShiftQualityParameter();

        public ClampedFloatParameter areaSize = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter areaFalloff = new ClampedFloatParameter(1f, 0.01f, 1f);
        public ClampedFloatParameter offset = new ClampedFloatParameter(0f, -1f, 1f);
        public ClampedFloatParameter angle = new ClampedFloatParameter(0f, 0f, 360f);
        
        public bool IsActive() => amount.value > 0f && this.active;

        public bool IsTileCompatible() => false;

        public static bool debug;
        
        [SerializeField]
        public Shader shader;

        private void Reset()
        {
            SerializeShader();
        }

        private bool SerializeShader()
        {
            bool wasSerialized = !shader;
            shader = Shader.Find(ShaderNames.TiltShift);

            return wasSerialized;
        }
    }
}