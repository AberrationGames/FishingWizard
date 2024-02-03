// SC Post Effects
// Staggart Creations http://staggart.xyz
// Copyright protected under the Unity Asset Store EULA

//Unity's RTHandle system is supposed to provide a better means of caching render targets
//There is however no way to determine when a renderpass has been removed from the render loop, making RT's linger
//Instead simply reallocate resources at the start of each frame
#define FORCE_REALLOCATION

using System;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if UNITY_2022_1_OR_NEWER
using RenderTarget = UnityEngine.Rendering.RTHandle;
#else
using RenderTarget = UnityEngine.Rendering.RenderTargetIdentifier;
#endif

namespace SCPE
{
    /// <summary>
    /// Base class for screen-space post processing through a ScriptableRenderPass
    /// </summary>
    /// <typeparam name="T">Related settings class</typeparam>
    #if UNITY_2021_1_OR_NEWER
    [DisallowMultipleRendererFeature]
    #endif
    public class PostEffectRenderer<T> : ScriptableRenderPass
    {
        public bool render;
        
        /// <summary>
        /// VolumeComponent settings instance
        /// </summary>
        public T volumeSettings;
        public EffectBaseSettings settings;

        #if UNITY_2021_2_OR_NEWER //No longer required to work with a copy (unless using the 2D renderer)
        private static bool is2D;
        //2D renderer and VR still requires a buffer copy, otherwise effects fail to render
        private bool RequireBufferCopy => is2D || xrRendering || this.renderPassEvent == RenderPassEvent.BeforeRenderingTransparents;
        #else
        private bool RequireBufferCopy => true;
        #endif
        private static bool isDeferred;

        private bool xrRendering;
        protected bool requiresDepth = false;
        protected bool requiresDepthNormals = false;

        protected string shaderName;
        private Shader shader;
        protected string ProfilerTag;
        protected Material Material;

        private static Material _BlitMaterial;
        private static Material BlitMaterial
        {
            get
            {
                if (!_BlitMaterial)
                {
                    _BlitMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Universal Render Pipeline/Blit"));
                }
                
                return _BlitMaterial;
            }
        }

        private static RTHandle cameraColorSource;
        protected RTHandle cameraColorTarget;

        private RenderTextureDescriptor cameraTargetRtDsc;
        private static RenderTextureDescriptor tempRTDesc;
        private int mainTexID = Shader.PropertyToID(TextureNames.Main);

        public bool reconstructDepthNormals;
        private int depthNormalsID = Shader.PropertyToID(TextureNames.DepthNormals);
        private static RTHandle cameraNormalsTexture;
        
        private readonly ProfilingSampler bufferCopyProfiler = new ProfilingSampler("Copy color");
        private readonly ProfilingSampler depthNormalsProfiler = new ProfilingSampler("Reconstruct normals from depth");

        internal string GetProfilerTag()
        {
            return shaderName.Replace(ShaderNames.PREFIX, "SCPE ");
        }

        private static int prevRendererID = -1;
        private static int currentRendererID = -1;
        
        private void DetermineRendererType(ScriptableRenderer renderer)
        {
            #if UNITY_2021_2_OR_NEWER
            //Check if the renderer has changed. May be switching between 2D and 3D, or Forward and Deferred
            currentRendererID = renderer.GetHashCode();
            
            if (currentRendererID != prevRendererID)
            {
                prevRendererID = currentRendererID;

                //Renderer type we're looking for is "Univeral2D", but of course that's been made internal. Assume anything that isn't 3D to be the 2D renderer
                is2D = renderer.GetType() != typeof(UniversalRenderer);

                if (is2D == false)
                {
                    ScriptableRendererData[] rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UniversalRenderPipeline.asset);
                    int defaultRendererIndex = (int)typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UniversalRenderPipeline.asset);
                    UniversalRendererData r = (UniversalRendererData)rendererDataList[defaultRendererIndex];
                    isDeferred = r.renderingMode == RenderingMode.Deferred;
                }
                
                #if SCPE_DEV
                Debug.Log($"[SCPE] Renderer changed or initialized. Type:{renderer.GetType()}. Deferred:{isDeferred}");
                #endif
            }
            #endif
        }

        //Always fetch the volume component first, then call this base implementation
        public virtual void Setup(ScriptableRenderer renderer, RenderingData renderingData)
        {
            render = true;
            
            if (volumeSettings == null)
            {
                render = false;
                return;
            }

            //Shaders are referenced on the corresponding volume profile asset. The render feature may be present, but the settings component may be absent in a build!
            if(!shader) shader = Shader.Find(shaderName);
            
            if (ShouldRenderForCamera(renderingData) == false)
            {
                render = false;
                return;
            }

            if (render)
            {
                DetermineRendererType(renderer);
            }
        }
        
        public RTHandle GetCameraTarget(ScriptableRenderer renderer)
        {
#if UNITY_2020_2_OR_NEWER
            //Fetched in CopyTargets function, no longer allowed from a ScriptableRenderFeature setup function (target may be not be created yet, or was disposed)
            return cameraColorTarget;
#else
            return renderer.cameraColorTarget;
#endif
        }

        public void SetCameraTarget(ScriptableRenderer renderer)
        {
            #if UNITY_2022_1_OR_NEWER
            cameraColorTarget = renderer.cameraColorTargetHandle;
            #endif
        }

        private bool IsAllowedCameraType(EffectBaseSettings.CameraTypeFlags flag)
        {
            return (settings.cameraTypes & flag) == flag;
        }
        
        /// <summary>
        /// Checks if post-processing pass should execute, based on current settings
        /// </summary>
        /// <returns></returns>
        public bool ShouldRenderForCamera(RenderingData renderingData)
        {
            if (renderingData.postProcessingEnabled == false && !settings.alwaysEnable) return false;
            
            #if UNITY_EDITOR
            if (renderingData.cameraData.camera.cameraType == CameraType.SceneView)
            {
                if (IsAllowedCameraType(EffectBaseSettings.CameraTypeFlags.SceneView) == false) return false;
                //Bail out if post processing is disabled in the scene view
                if (SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.sceneViewState.showImageEffects == false) return false;
            }
			
			#if UNITY_2021_2_OR_NEWER
            //Don't interfere with the Rendering Debugger
            if (Shader.IsKeywordEnabled(ShaderKeywordStrings.DEBUG_DISPLAY)) return false;
            #endif
            #endif
            
            if (renderingData.cameraData.camera.cameraType == CameraType.Game)
            {
                if(renderingData.cameraData.camera.hideFlags != HideFlags.None && IsAllowedCameraType(EffectBaseSettings.CameraTypeFlags.Hidden) == false) return false;
                
                if(renderingData.cameraData.renderType == CameraRenderType.Base && IsAllowedCameraType(EffectBaseSettings.CameraTypeFlags.GameBase) == false) return false;
                if(renderingData.cameraData.renderType == CameraRenderType.Overlay && IsAllowedCameraType(EffectBaseSettings.CameraTypeFlags.GameOverlay) == false) return false;
            } 
            
            if(renderingData.cameraData.camera.cameraType == CameraType.Reflection && IsAllowedCameraType(EffectBaseSettings.CameraTypeFlags.Reflection) == false) return false;
            if(renderingData.cameraData.camera.cameraType == CameraType.Preview && IsAllowedCameraType(EffectBaseSettings.CameraTypeFlags.Preview) == false) return false;

            return true;
        }
        
        private void CreateMaterialIfNull(ref Material material, Shader m_shader)
        {
            if (material) return;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!m_shader)
            {
                Debug.LogError("[SC Post Effects] Shader with the name <i>" + shaderName + "</i> could not be found, this means it was not included in the build. This will happen when effects are entirely initialized through scripting. Be sure to reference the shader somewhere.");
                return;
            }
            #endif
            
            material = CoreUtils.CreateEngineMaterial(m_shader);
            //Material cannot be serialized anyway
            material.hideFlags = HideFlags.DontSave;
            material.name = m_shader.name;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            #if ENABLE_VR_MODULE && ENABLE_XR_MODULE
            xrRendering = renderingData.cameraData.xrRendering;
            #else
            xrRendering = false;
            #endif
        }

        /// <summary>
        /// Sets up MainTex RenderTarget and depth normals if needed. Check if settings are valid before calling this base implementation
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="cameraTextureDescriptor"></param>
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
#if UNITY_2020_1_OR_NEWER
            //Copy is required so it remains valid over the course of the render loop
            cameraTargetRtDsc = cameraTextureDescriptor;
            
            //No need for MSAA
            cameraTargetRtDsc.msaaSamples = 1;
#else
            cameraTargetRtDsc = cameraTextureDescriptor;
#endif
            
            ConfigurePass(cmd, cameraTargetRtDsc);
        }

        private static RTHandle AllocateRT(RenderTextureDescriptor cameraTextureDescriptor, GraphicsFormat format, FilterMode filterMode, string name, int downsampling = 1)
        {
            #if SCPE_DEV
            //Debug.LogFormat("Allocating {0}. Dimensions:{1}", name, cameraTextureDescriptor.dimension);
            #endif
            
            return RTHandles.Alloc(cameraTextureDescriptor.width / downsampling, cameraTextureDescriptor.height / downsampling, cameraTextureDescriptor.volumeDepth, DepthBits.None, format, filterMode, TextureWrapMode.Clamp, cameraTextureDescriptor.dimension, name: name);
        }

        public static void ReleaseRT(RTHandle handle)
        {
            #if SCPE_DEV
            //Debug.LogFormat("Releasing {0}", handle.name);
            #endif
            
            RTHandles.Release(handle);
        }

        private static bool RTHandleNeedsReAlloc(RTHandle handle, in RenderTextureDescriptor descriptor, in string name)
        {
            #if FORCE_REALLOCATION
            return true;
            #else
            //#if UNITY_2022_1_OR_NEWER
            //Using this results in a depth texture constantly being allocated?!
            //return RenderingUtils.ReAllocateIfNeeded(ref handle, descriptor);
            //#else
            //If not ever being allocated
            if (handle == null || handle.rt == null)
            {
                #if SCPE_DEV
                Debug.Log($"RTHandle {name} null, allocating");
                #endif
                return true;
            }
            
            //Resolution changes
            if ((handle.rt.width != descriptor.width || handle.rt.height != descriptor.height))
            {
                #if SCPE_DEV
                Debug.Log($"{name} resolution changed. Source:{descriptor.width}x{descriptor.height}. Current:{handle.rt.width}x{handle.rt.height}");
                #endif
                return true;
            }

            //In case XR is initialized at some point
            if (handle.rt.descriptor.dimension != descriptor.dimension)
            {
                #if SCPE_DEV
                Debug.Log($"{name} dimensions changed. Source:{descriptor.dimension}. Current:{handle.rt.descriptor.dimension}");
                #endif
                return true;
            }

            return false;
            #endif
        }

        public static RTHandle GetTemporaryRT(ref RTHandle handle, RenderTextureDescriptor cameraTextureDescriptor, GraphicsFormat format, FilterMode filterMode, string name, int downsampling = 1)
        {
            tempRTDesc = cameraTextureDescriptor;
            
            if (downsampling > 1)
            {
                //Trick the descriptor to having the same resolution division.
                //Otherwise downsampled RenderTargets will always be re-allocated
                tempRTDesc.width /= downsampling;
                tempRTDesc.height /= downsampling;
            }

            //Only re-allocate if null or resolution no longer matches.
            if (RTHandleNeedsReAlloc(handle, tempRTDesc, name))
            {
                #if SCPE_DEV && !FORCE_REALLOCATION
                Debug.Log("Releasing RT " + name);
                #endif
                
                //Note: function does a null check, needed for the first allocation
                if(handle != null) ReleaseRT(handle);
                
                //Using the original descriptor here, which has unchanged resolution
                handle = AllocateRT(cameraTextureDescriptor, format, filterMode, name, downsampling);
            }
            
            //Unclear when and where RenderTarget's should be released. Doing so at the end of every frame defeats the point of caching!
            //There isn't any function being called when a pass is deactivated or removed.
            
            return handle;
        }

        private ScriptableRenderPassInput requirements;
        
        protected virtual void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            CreateMaterialIfNull(ref Material, shader);
            
            if (RequireBufferCopy)
            {
                cameraColorSource = GetTemporaryRT(ref cameraColorSource, cameraTextureDescriptor, cameraTextureDescriptor.graphicsFormat, FilterMode.Point, this.ProfilerTag + "_CameraColorSource");
            }

            #if UNITY_2020_2_OR_NEWER
            //Inform the render pipeline which pre-passes are required
            requirements = ScriptableRenderPassInput.None;
            
            if(requiresDepth) requirements = ScriptableRenderPassInput.Depth;
            if(requiresDepthNormals && !reconstructDepthNormals) requirements |= ScriptableRenderPassInput.Normal;
            
            ConfigureInput(requirements);
            
            CoreUtils.SetKeyword(Material, ShaderKeywords.ReconstructedDepthNormals, reconstructDepthNormals);
            #endif
            
            #if UNITY_2021_2_OR_NEWER
            //GBuffer2 contains normals already
            cmd.SetGlobalInt(ShaderParameters._DeferredRendering, isDeferred ? 1 : 0);
            #endif

            if (requiresDepthNormals)
            {
                #if UNITY_2020_2_OR_NEWER
                if(reconstructDepthNormals && !isDeferred)
                #endif
                {
                    if (!DepthNormalsShader) DepthNormalsShader = Shader.Find(ShaderNames.DepthNormals);
                
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (!DepthNormalsShader)
                    {
                        Debug.LogError("[SC Post Effects] Shader with the name <i>" + shaderName + "</i> could not be found, this means it was not included in the build. This will happen when effects are entirely initialized through scripting. Be sure to reference the shader somewhere.");
                        return;
                    }
                    #endif
                
                    CreateMaterialIfNull(ref DepthNormalsMat, DepthNormalsShader);

                    cameraNormalsTexture = GetTemporaryRT(ref cameraNormalsTexture, cameraTextureDescriptor, GraphicsFormat.R8G8_UNorm, FilterMode.Point, TextureNames.DepthNormals);
                    cmd.SetGlobalTexture(depthNormalsID, cameraNormalsTexture);
                }
            }
        }

        protected CommandBuffer GetCommandBuffer(ref RenderingData renderingData)
        {
            #if UNITY_2022_1_OR_NEWER
            //URP 13 supposedly will use a shared CommandBuffer passed along in renderingData
            #else
            #endif
            
            return CommandBufferPool.Get(ProfilerTag);
        }

        /// <summary>
        /// Compose and execute command buffer. No need to call base implementation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }

        private void GetCameraColorTarget(RenderingData renderingData)
        {
            #if UNITY_2020_2_OR_NEWER //URP 10+
                #if UNITY_2022_1_OR_NEWER
                cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                #else
                //cameraColorTarget = GetTemporaryRT(ref cameraColorTarget, cameraTargetRtDsc, cameraTargetRtDsc.graphicsFormat, FilterMode.Point, "CameraColorTarget");
                cameraColorTarget?.Release();
                cameraColorTarget = RTHandles.Alloc(renderingData.cameraData.renderer.cameraColorTarget);
                #endif
            #endif
        }
        
        /// <summary>
        /// Copies the color, depth and depth normals if required
        /// </summary>
        /// <param name="cmd"></param>
        protected void CopyTargets(CommandBuffer cmd, RenderingData renderingData)
        {
            //Color target can now only be fetched inside a ScriptableRenderPass
            GetCameraColorTarget(renderingData);

            if (RequireBufferCopy)
            {
                using (new ProfilingScope(cmd, bufferCopyProfiler))
                {
                    BlitCopy(cmd, cameraColorTarget, cameraColorSource);
                }
            }

            GenerateDepthNormals(this, cmd);
        }
        
        private Material DepthNormalsMat;
        private static Shader DepthNormalsShader;
        
        /// <summary>
        /// Reconstructs view-space normals from depth texture
        /// </summary>
        /// <param name="pass"></param>
        /// <param name="cmd"></param>
        /// <param name="dest"></param>
        private void GenerateDepthNormals(ScriptableRenderPass pass, CommandBuffer cmd)
        {
            if (!requiresDepthNormals || isDeferred) return;
            
            #if UNITY_2020_2_OR_NEWER
            //Using depth-normals pre-pass
            if(reconstructDepthNormals == false) return;
            #endif

            using (new ProfilingScope(cmd, depthNormalsProfiler))
            {
                Blit(pass, cmd, cameraNormalsTexture /* not actually used */, cameraNormalsTexture, DepthNormalsMat, 0);
            }
        }

        protected void BlitCopy(CommandBuffer cmd, RenderTarget source, RenderTarget dest)
        {
            cmd.SetGlobalTexture(TextureNames.Source, source);
            
            Blit(this, cmd, source, dest, BlitMaterial, 0);
        }

        private static Vector4 ScaleBias = new Vector4(1, 1, 0, 0);

        /// <summary>
        /// Wrapper for ScriptableRenderPass.Blit but allows shaders to keep using _MainTex across render pipelines
        /// </summary>
        /// <param name="cmd">Command buffer to record command for execution.</param>
        /// <param name="source">Source texture or target identifier to blit from.</param>
        /// <param name="destination">Destination texture or target identifier to blit into. This becomes the renderer active render target.</param>
        /// <param name="material">Material to use.</param>
        /// <param name="passIndex">Shader pass to use. Default is 0.</param>
        protected void Blit(ScriptableRenderPass pass, CommandBuffer cmd, RenderTarget source, RenderTarget target, Material mat, int passIndex, bool clearColor = false)
        {
            cmd.SetGlobalTexture(mainTexID, source);
            
            cmd.SetRenderTarget(target, 0, CubemapFace.Unknown, -1);
            //cmd.SetRenderTarget(target, RenderBufferLoadAction.Load,RenderBufferStoreAction.DontCare,  RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
            //CoreUtils.SetRenderTarget(cmd, target, BuiltinRenderTextureType.CurrentActive, ClearFlag.None, 0, CubemapFace.Unknown, -1);
            if(clearColor) cmd.ClearRenderTarget(true, true, Color.clear);
  
            //Required for correct UV calculations in the vertex shader
            cmd.SetGlobalVector(ShaderParameters._BlitScaleBiasRt, ScaleBias);
            cmd.SetGlobalVector(ShaderParameters._BlitScaleBias, ScaleBias);
            
            if (xrRendering)
            {
                //_USE_DRAW_PROCEDURAL vertex shader code path
                cmd.DrawProcedural(Matrix4x4.identity, mat, passIndex, MeshTopology.Quads, 4, 1, null);
            }
            else
            {
                cmd.Blit(source, target, mat, passIndex);
                //pass.Blit(cmd, source, target, mat, passIndex);
            }
        }

        /// <summary>
        /// Blits to the camera color target and executes the command buffer
        /// </summary>
        protected void FinalBlit(ScriptableRenderPass pass, ScriptableRenderContext context, CommandBuffer cmd, RenderingData renderingData, int passIndex)
        {
            if (RequireBufferCopy)
            {
                Blit(pass, cmd, cameraColorSource, cameraColorTarget, Material, passIndex);
            }
            //In <URP 12 'RequireBufferCopy' will be true anyway, so always a valid code path
            else
            {
                #if UNITY_2021_2_OR_NEWER && !UNITY_2022_1_OR_NEWER
                //Input, in case of the swap-buffer Blit function, its the camera target
                cmd.SetGlobalTexture(mainTexID, cameraColorTarget);
                
                pass.Blit(cmd, ref renderingData, Material, passIndex);
                #endif
                
                #if UNITY_2022_1_OR_NEWER
                cmd.SetGlobalTexture(mainTexID, cameraColorTarget);
                
                //Throws a warning about the target being null. As of this version the GetCameraColorFrontBuffer does indeed return null.
                //The swap-buffer behaviour seems to have been removed, but no alternative is present at the moment :/
                pass.Blit(cmd, ref renderingData, Material, passIndex);
                #endif
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        
        /// <summary>
        /// Releases the basic resources used by any effect. Cleanup be effect specific resources before calling the base implementation!
        /// </summary>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //Actually don't want to release this, otherwise the next effect that comes along simply re-allocates it.
            //But the whole 'custom' post processing pipeline provides no means to know when an effect is removed from it.
            //To avoid memory leaks, RT's are simply disposed and re-allocated as an alternative.
            if (RequireBufferCopy && ShouldReleaseRT())
            {
                ReleaseRT(cameraColorSource);
            }
            
            if (requiresDepthNormals && ShouldReleaseRT()) ReleaseRT(cameraNormalsTexture);
        }

        private static bool isPlaying;
        //Clearing render targets in edit mode makes them null when needed to be used
        //Can't explain why exactly this happens, only viable solution is to simply let them linger
        protected bool ShouldReleaseRT()
        {
            isPlaying = Application.isPlaying;
            
            #if UNITY_EDITOR
            isPlaying &= !EditorApplication.isPaused;
            #endif
            
            return isPlaying;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(Material);
            
            if (requiresDepthNormals) CoreUtils.Destroy(DepthNormalsMat);
        }
        
        #region Misc
        private static Matrix4x4 lightToLocalMatrix;
        
        public void SetMainLightProjection(CommandBuffer cmd, RenderingData renderingData)
        {
            if (renderingData.lightData.mainLightIndex > -1)
            {
                VisibleLight mainLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex];
    
                if (mainLight.lightType == LightType.Directional)
                {
                    lightToLocalMatrix = mainLight.light.transform.worldToLocalMatrix;
                    
                    //Ensure the position value stays zero, otherwise the projection moves with the light whilst only the rotation is of importance
                    //lightToLocalMatrix.SetColumn(3, Vector4.zero);
                    
                    cmd.SetGlobalMatrix(ShaderParameters.unity_WorldToLight, lightToLocalMatrix);
                }
            }
        }

        private static readonly int viewProjection = Shader.PropertyToID("viewProjection");
        private static readonly int viewMatrix = Shader.PropertyToID("viewMatrix");
        
        private static Matrix4x4[] s_viewProjectionMatrices = new Matrix4x4[2];
        private static readonly int viewProjectionArray = Shader.PropertyToID("viewProjectionArray");
        
        /// <summary>
        /// unity_MatrixVP and unity_MatrixV aren't consistently set up, instead it can be redone here. The URP.hlsl redefines UNITY_MATRIX_VP and UNITY_MATRIX_V to this
        /// </summary>
        protected void SetViewProjectionMatrixUniforms(CommandBuffer cmd, in CameraData cameraData)
        {
            //Broken in older versions
            #if UNITY_2022_1_OR_NEWER && !UNITY_2022_2_OR_NEWER
            if (xrRendering)
            {
                //Can throw an error if XR was not yet completely initialized
                for (int eyeIndex = 0; eyeIndex < TextureXR.slices; eyeIndex++)
                {
                    s_viewProjectionMatrices[eyeIndex] = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(eyeIndex), cameraData.IsCameraProjectionMatrixFlipped()) * cameraData.GetViewMatrix(eyeIndex);
                }

                cmd.SetGlobalMatrixArray(viewProjectionArray, s_viewProjectionMatrices);
            }
            else
            #endif
            {
                cmd.SetGlobalMatrix(viewProjection, GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), cameraData.IsCameraProjectionMatrixFlipped()) * cameraData.GetViewMatrix());
            }
            
            cmd.SetGlobalMatrix(viewMatrix, cameraData.camera.cameraToWorldMatrix);
        }
        #endregion
    }
}