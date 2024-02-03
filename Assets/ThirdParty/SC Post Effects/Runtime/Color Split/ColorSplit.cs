using UnityEngine.Rendering.Universal;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCPE
{
    [Serializable, VolumeComponentMenu("SC Post Effects/Retro/Color Split")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class ColorSplit : VolumeComponent, IPostProcessComponent
    {
        public enum SplitMode
        {
            [InspectorName("Horizontal")]
            Single,
            [InspectorName("Horizontal + Vertical")]
            Double
        }

        [Serializable]
        public sealed class SplitModeParam : VolumeParameter<SplitMode> { }

        [Tooltip("Box filtered methods provide a subtle blur effect and are less efficient")]
        public SplitModeParam mode = new SplitModeParam { value = SplitMode.Single };

        [Range(0f, 1f), Tooltip("The amount by which the color channels offset")]
        public FloatParameter offset = new FloatParameter(0f);
        
        [Tooltip("0=Full screen. 1=Limit to screen edges")]
        public ClampedFloatParameter edgeMasking = new ClampedFloatParameter(0f, 0f, 1f);

        [Range(0f, 3f), Tooltip("Luminance threshold, pixels above this threshold will contribute to the effect")]
        public ClampedFloatParameter luminanceThreshold = new ClampedFloatParameter(0f, 0f, 3f);
        
        public bool IsActive() => offset.value > 0f && this.active;

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
            shader = Shader.Find(ShaderNames.ColorSplit);

            return wasSerialized;
        }
    }
}