using UnityEngine.Rendering.Universal;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace SCPE
{
    [Serializable, VolumeComponentMenu("SC Post Effects/Screen/Refraction")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class Refraction : VolumeComponent, IPostProcessComponent
    {
        [FormerlySerializedAs("refractionTex")]
        [Tooltip("Takes a normal map to perturb the image")]
        public TextureParameter normalMap = new TextureParameter(null);
        
        [Range(0f, 1f), Tooltip("Amount")]
        public ClampedFloatParameter amount = new ClampedFloatParameter(0f,0f,1f);
        public ColorParameter tint = new ColorParameter(new Color(1,1,1, 0.1f));

        public bool IsActive() => amount.value > 0f && normalMap.value != null && this.active;

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
            shader = Shader.Find(ShaderNames.Refraction);

            return wasSerialized;
        }
    }
}