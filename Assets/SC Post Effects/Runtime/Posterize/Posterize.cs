using UnityEngine.Rendering.Universal;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCPE
{
    [Serializable, VolumeComponentMenu("SC Post Effects/Retro/Posterize")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class Posterize : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter hsvMode = new BoolParameter(false);

        [Range(0, 256)]
        public ClampedIntParameter levels = new ClampedIntParameter(256, 1, 256);

        [Header("Levels")]
        [Range(2, 256)]
        public ClampedIntParameter hue = new ClampedIntParameter(256, 2, 256);
        [Range(2, 256)]
        public ClampedIntParameter saturation = new ClampedIntParameter(256, 2, 256);
        [Range(2, 256)]
        public ClampedIntParameter value = new ClampedIntParameter(256, 2, 256);

        public bool IsActive() => (!hsvMode.value && levels.value < 256) || (hsvMode.value && (hue.value < 256 || saturation.value < 256 || value.value < 256)) && this.active;

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
            shader = Shader.Find(ShaderNames.Posterize);

            return wasSerialized;
        }
    }
}