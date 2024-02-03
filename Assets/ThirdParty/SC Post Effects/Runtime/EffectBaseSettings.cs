using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SCPE
{
    [System.Serializable]
    public class EffectBaseSettings
    {
        [Flags]
        public enum CameraTypeFlags
        {
            None = 0,
            [InspectorName("Game (Base)")]
            GameBase = 1,
            [InspectorName("Game (Overlay)")]
            GameOverlay = 2,
            [InspectorName("Scene View")]
            SceneView = 4,
            [InspectorName("Preview")]
            Preview = 8,
            [InspectorName("Reflections")]
            Reflection = 16,
            [InspectorName("Hidden (HideFlags)")]
            Hidden = 32
        }

        public enum ExecutionOrder
        {
            [InspectorName("Before Unity's post processing effects")]
            BeforePostProcessing,
            [InspectorName("After Unity's post processing effects")]
            AfterPostProcessing
        }
        
        #if UNITY_2021_2_OR_NEWER
        [Tooltip("Configure the effect to render either before/after Unity's built-in post processing.")]
        public ExecutionOrder executionOrder = ExecutionOrder.BeforePostProcessing;
        #endif
        
        [Tooltip("Effect will render, even if the camera has post-processing disabled")]
        public bool alwaysEnable;
        
        [Tooltip("Configure which camera types the effect is allowed to render on when using camera stacking" +
                 "\n\nNote that some depth-based effects will not work with camera stacking, due to how the stacking system handles the depth texture")]
        public CameraTypeFlags cameraTypes = CameraTypeFlags.GameBase | CameraTypeFlags.SceneView;

        public EffectBaseSettings(bool enableInSceneView = true)
        {
            if(!enableInSceneView) this.cameraTypes = CameraTypeFlags.GameBase | CameraTypeFlags.GameOverlay;
            else this.cameraTypes = CameraTypeFlags.GameBase | CameraTypeFlags.SceneView;
        }

        public RenderPassEvent GetInjectionPoint()
        {
            #if UNITY_2021_2_OR_NEWER
            return executionOrder == EffectBaseSettings.ExecutionOrder.BeforePostProcessing ? RenderPassEvent.BeforeRenderingPostProcessing : RenderPassEvent.AfterRenderingPostProcessing;
            #else
            //No swap buffer system
            return RenderPassEvent.BeforeRenderingPostProcessing;
            #endif
        }
    }

    #if !UNITY_2023_1_OR_NEWER
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SupportedOnRenderPipelineAttribute : Attribute
    {
        public SupportedOnRenderPipelineAttribute(params System.Type[] renderPipeline) {}
    }
    #endif
}