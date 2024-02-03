using UnityEngine.Rendering.Universal;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCPE
{
    [Serializable, VolumeComponentMenu("SC Post Effects/Environment/Caustics")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class Caustics : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public TextureParameter causticsTexture = new TextureParameter(null);
        public FloatParameter brightness = new FloatParameter(1f);
        public ClampedFloatParameter luminanceThreshold = new ClampedFloatParameter(0f, 0f, 2f);
        public BoolParameter projectFromSun = new BoolParameter(false);
        
        public FloatParameter minHeight = new FloatParameter(-5f);
        public ClampedFloatParameter minHeightFalloff = new ClampedFloatParameter(10f, 0.01f, 64f);
        
        public FloatParameter maxHeight = new FloatParameter(0f);
        public ClampedFloatParameter maxHeightFalloff = new ClampedFloatParameter(10f, 0.01f, 64f);
        
        public ClampedFloatParameter size = new ClampedFloatParameter(0.5f, 0.1f, 1f);
        public ClampedFloatParameter speed = new ClampedFloatParameter(0.2f, 0f, 1f);
        
        public BoolParameter distanceFade = new BoolParameter(false);
        public FloatParameter startFadeDistance = new FloatParameter(0f);
        public FloatParameter endFadeDistance = new FloatParameter(200f);
        
        public bool IsActive() => intensity.value > 0f && this.active;

        public bool IsTileCompatible() => false;
        
        [SerializeField]
        public Shader shader;

        private void Reset()
        {
            SerializeShader();
            
            //Auto assign default texture
            #if UNITY_EDITOR
            if (causticsTexture.value == null)
            {
                causticsTexture.overrideState = true;
                causticsTexture.value = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(UnityEditor.AssetDatabase.GUIDToAssetPath("c04a5fb5ba4f3ee4489d7a234aba5d3c"));
            }
            #endif
        }
        
        private bool SerializeShader()
        {
            bool wasSerialized = !shader;
            shader = Shader.Find(ShaderNames.Caustics);

            return wasSerialized;
        }
    }
}