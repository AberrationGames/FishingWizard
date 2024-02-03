using UnityEngine;

namespace SCPE
{
    public static class ShaderNames
    {
        public const string PREFIX = "Hidden/SC Post Effects/";
        
        public const string TEST = PREFIX + "Test";
        public const string AO2D = PREFIX + "Ambient Occlusion 2D";
        public const string BlackBars = (PREFIX + "Black Bars");
        public const string Blur = PREFIX + "Blur";
        public const string Caustics = PREFIX + "Caustics";
        public const string CloudShadows = PREFIX + "Cloud Shadows";
        public const string ColorSplit = PREFIX + "Color Split";
        public const string Colorize = PREFIX + "Colorize";
        public const string Danger = PREFIX + "Danger";
        public const string Dithering = PREFIX + "Dithering";
        public const string DoubleVision = PREFIX + "Double Vision";
        public const string EdgeDetection = PREFIX + "Edge Detection";
        public const string Fog = PREFIX + "Fog";
        public const string Gradient = PREFIX + "Gradient";
        public const string HueShift3D = PREFIX + "3D Hue Shift";
        public const string Kaleidoscope = PREFIX + "Kaleidoscope";
        public const string Kuwahara = PREFIX + "Kuwahara";
        public const string LensFlares = PREFIX + "Lensflares";
        public const string LightStreaks = PREFIX + "Light Streaks";
        public const string LUT = PREFIX + "LUT";
        public const string Mosaic = PREFIX + "Mosaic";
        public const string Overlay = PREFIX + "Overlay";
        public const string Pixelize = PREFIX + "Pixelize";
        public const string Posterize = PREFIX + "Posterize";
        public const string RadialBlur = PREFIX + "Radial Blur";
        public const string Refraction = PREFIX + "Refraction";
        public const string Ripples = PREFIX + "Ripples";
        public const string Scanlines = PREFIX + "Scanlines";
        public const string Sharpen = PREFIX + "Sharpen";
        public const string Sketch = PREFIX + "Sketch";
        public const string SpeedLines = PREFIX + "SpeedLines";
        public const string Sunshafts = PREFIX + "Sun Shafts";
        public const string TiltShift = PREFIX + "Tilt Shift";
        public const string Tracers = PREFIX + "Tracers";
        public const string Transition = PREFIX + "Transition";
        public const string TubeDistortion = PREFIX + "Tube Distortion";

        public const string DepthNormals = PREFIX + "DepthNormals";
    }

    internal static class ShaderKeywords
    {
        public const string ReconstructedDepthNormals = "_RECONSTRUCT_NORMAL";
    }

    public static class ShaderParameters
    {
        public static int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
        public static int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        public static int _DeferredRendering = Shader.PropertyToID("_DeferredRendering");
        
        public static int unity_WorldToLight = Shader.PropertyToID("unity_WorldToLight");

        public static int Params = Shader.PropertyToID("_Params");
        public static int FadeParams = Shader.PropertyToID("_FadeParams");
        public static int BlurOffsets = Shader.PropertyToID("_BlurOffsets");
        public static int BlurRadius = Shader.PropertyToID("_BlurRadius");
    }

    internal static class TextureNames
    {
        public const string Main = "_MainTex";
    #if UNITY_2022_2_OR_NEWER
        public const string Source = "_BlitTexture";
    #else
        public const string Source = "_SourceTex";
    #endif
        public const string DepthTexture = "_CameraDepthTexture";
        public const string DepthNormals = "_CameraDepthNormalsTexture";
        public const string FogSkyboxTex = "_SkyboxTex";
    }
}