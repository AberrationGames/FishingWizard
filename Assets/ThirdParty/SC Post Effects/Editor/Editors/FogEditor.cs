using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEditor.AnimatedValues;
using UnityEditor;
using UnityEngine;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(Fog))]
    sealed class FogEditor : VolumeComponentEditor
    {
        SerializedDataParameter useSceneSettings;
        SerializedDataParameter fogMode;
        SerializedDataParameter fogDensity;
        SerializedDataParameter fogStartDistance;
        SerializedDataParameter fogEndDistance;

        SerializedDataParameter colorMode;

        SerializedDataParameter fogColor;
        SerializedDataParameter fogColorGradient;
        SerializedDataParameter gradientDistance;
        SerializedDataParameter gradientUseFarClipPlane;

        SerializedDataParameter distanceFog;
        SerializedDataParameter distanceDensity;
        SerializedDataParameter useRadialDistance;

        SerializedDataParameter horizonMinMax;
        SerializedDataParameter skyboxInfluence;

        SerializedDataParameter enableDirectionalLight;
        SerializedDataParameter useLightDirection;
        SerializedDataParameter useLightColor;
        SerializedDataParameter useLightIntensity;
        SerializedDataParameter lightColor;
        SerializedDataParameter lightDirection;
        SerializedDataParameter lightIntensity;
        SerializedDataParameter lightExponent;

        SerializedDataParameter heightFog;
        SerializedDataParameter height;
        SerializedDataParameter heightDensity;

        SerializedDataParameter heightFogNoise;
        SerializedDataParameter heightNoiseTex;
        SerializedDataParameter heightNoiseSize;
        SerializedDataParameter heightNoiseStrength;
        SerializedDataParameter heightNoiseSpeed;

        SerializedDataParameter lightScattering;
        SerializedDataParameter scatterIntensity;
        SerializedDataParameter scatterDiffusion;
        SerializedDataParameter scatterThreshold;
        SerializedDataParameter scatterSoftKnee;

        private float animSpeed = 4f;
        AnimBool m_showControls;
        AnimBool m_showHeight;
        AnimBool m_showSun;
        AnimBool m_showScattering;

        private bool isSetup;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<Fog>(serializedObject);
            isSetup = AutoSetup.ValidEffectSetup<FogRenderer>();

            useSceneSettings = Unpack(o.Find(x => x.useSceneSettings));
            fogMode = Unpack(o.Find(x => x.fogMode));
            fogDensity = Unpack(o.Find(x => x.globalDensity));
            fogStartDistance = Unpack(o.Find(x => x.fogStartDistance));
            fogDensity = Unpack(o.Find(x => x.globalDensity));
            fogEndDistance = Unpack(o.Find(x => x.fogEndDistance));
            colorMode = Unpack(o.Find(x => x.colorSource));

            fogColor = Unpack(o.Find(x => x.fogColor));
            fogColorGradient = Unpack(o.Find(x => x.fogColorGradient));
            gradientDistance = Unpack(o.Find(x => x.gradientDistance));
            gradientUseFarClipPlane = Unpack(o.Find(x => x.gradientUseFarClipPlane));
            distanceFog = Unpack(o.Find(x => x.distanceFog));
            distanceDensity = Unpack(o.Find(x => x.distanceDensity));
            useRadialDistance = Unpack(o.Find(x => x.useRadialDistance));
            heightFog = Unpack(o.Find(x => x.heightFog));
            height = Unpack(o.Find(x => x.height));
            heightDensity = Unpack(o.Find(x => x.heightDensity));
            heightFogNoise = Unpack(o.Find(x => x.heightFogNoise));
            heightNoiseTex = Unpack(o.Find(x => x.heightNoiseTex));
            heightNoiseSize = Unpack(o.Find(x => x.heightNoiseSize));
            heightNoiseStrength = Unpack(o.Find(x => x.heightNoiseStrength));
            heightNoiseSpeed = Unpack(o.Find(x => x.heightNoiseSpeed));

            horizonMinMax = Unpack(o.Find(x => x.horizonMinMax));
            skyboxInfluence = Unpack(o.Find(x => x.skyboxInfluence));

            enableDirectionalLight = Unpack(o.Find(x => x.enableDirectionalLight));
            useLightDirection = Unpack(o.Find(x => x.useLightDirection));
            useLightColor = Unpack(o.Find(x => x.useLightColor));
            useLightIntensity = Unpack(o.Find(x => x.useLightIntensity));
            lightColor = Unpack(o.Find(x => x.lightColor));
            lightDirection = Unpack(o.Find(x => x.lightDirection));
            lightIntensity = Unpack(o.Find(x => x.lightIntensity));
            lightExponent = Unpack(o.Find(x => x.lightExponent));
            
            lightScattering = Unpack(o.Find(x => x.lightScattering));
            scatterIntensity = Unpack(o.Find(x => x.scatterIntensity));
            scatterDiffusion = Unpack(o.Find(x => x.scatterDiffusion));
            scatterThreshold = Unpack(o.Find(x => x.scatterThreshold));
            scatterSoftKnee = Unpack(o.Find(x => x.scatterSoftKnee));

            m_showControls = new AnimBool(true);
            m_showControls.valueChanged.AddListener(Repaint);
            m_showControls.speed = animSpeed;

            m_showHeight = new AnimBool(true);
            m_showHeight.valueChanged.AddListener(Repaint);
            m_showHeight.speed = animSpeed;

            m_showSun = new AnimBool(true);
            m_showSun.valueChanged.AddListener(Repaint);
            m_showSun.speed = animSpeed;

            m_showScattering = new AnimBool(true);
            m_showScattering.valueChanged.AddListener(Repaint);
            m_showScattering.speed = animSpeed;
        }

        #if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Screen-Space Fog (" + colorMode.value.enumDisplayNames[colorMode.value.intValue] + ")");
        }
        #else
        public override string GetDisplayTitle()
        {
            return "Screen-Space Fog (" + colorMode.value.enumDisplayNames[colorMode.value.intValue] + ")";
        }
        #endif

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("fog");

            SCPE_GUI.DisplaySetupWarning<FogRenderer>(ref isSetup, false);

            if (RenderSettings.fog)
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.HelpBox("Fog is currently enabled in the active scene, resulting in an overlapping fog effect", MessageType.Warning);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Disable scene fog"))
                        {
                            RenderSettings.fog = false;
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        }
                        GUILayout.FlexibleSpace();
                    }


                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
            }
            
            PropertyField(fogDensity);
            SCPE_GUI.DisplayIntensityWarning(fogDensity);
            
            EditorGUILayout.Space();

            PropertyField(useSceneSettings);
            EditorGUILayout.Space();

            m_showControls.target = !useSceneSettings.value.boolValue;
            if (EditorGUILayout.BeginFadeGroup(m_showControls.faded))
            {
                PropertyField(fogMode);
                PropertyField(fogStartDistance);
                if (fogMode.value.intValue == 1)
                {
                    PropertyField(fogEndDistance);
                }

                PropertyField(colorMode);
                if (colorMode.value.intValue == 0)
                {
                    PropertyField(fogColor);
                }
                else if (colorMode.value.intValue == 1) //gradient
                {
                    PropertyField(fogColorGradient);
                    SCPE_GUI.DisplayTextureOverrideWarning(fogColorGradient.overrideState.boolValue);

                    if (fogColorGradient.value.objectReferenceValue)
                    {
                        SCPE.CheckGradientImportSettings(fogColorGradient.value.objectReferenceValue);

                        GUILayout.Space(5f);
                        //Gradient preview
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Near", GUILayout.MaxWidth(32f));
                            Rect rect = GUILayoutUtility.GetRect(1, 12, "TextField");
                            EditorGUI.DrawTextureTransparent(rect, fogColorGradient.value.objectReferenceValue as Texture2D);
                            EditorGUILayout.LabelField("Far", GUILayout.MaxWidth(30f));
                        }

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUI.BeginDisabledGroup(!gradientUseFarClipPlane.value.boolValue);
                            {
                                PropertyField(gradientDistance, new GUIContent("Distance"));
                            }
                            EditorGUI.EndDisabledGroup();
                            gradientUseFarClipPlane.overrideState.boolValue = true;
                            gradientUseFarClipPlane.value.boolValue = !GUILayout.Toggle(!gradientUseFarClipPlane.value.boolValue, new GUIContent("Automatic", "Distance will be set by the camera's far clipping plane"), EditorStyles.miniButton);
                        }
                        EditorGUILayout.EndHorizontal();

                        //EditorGUILayout.HelpBox("In scene view, the gradient will appear to look different due to the camera's dynamic far clipping value", MessageType.None);
                    }
                }
                else if (colorMode.value.intValue == 2) //Skybox
                {

                }
            }
            EditorGUILayout.EndFadeGroup();

            PropertyField(distanceFog);
            if (colorMode.value.intValue == 1 && distanceFog.value.boolValue == false && heightFog.value.boolValue)
            {
                EditorGUILayout.HelpBox("Distance fog must be enabled when using the Gradient Texture color mode. Height fog only will not be colored correctly", MessageType.Warning);
            }
            if (distanceFog.value.boolValue)
            {
                PropertyField(useRadialDistance, new GUIContent("Radial"));
                PropertyField(distanceDensity);
            }

            EditorGUILayout.LabelField("Skybox", EditorStyles.boldLabel);
            float minBrightness = horizonMinMax.value.vector2Value.x;
            float maxBrightness = horizonMinMax.value.vector2Value.y;
            
            using (new EditorGUILayout.HorizontalScope())
            {
                // Override checkbox
                var overrideRect = GUILayoutUtility.GetRect(17f, 17f, GUILayout.ExpandWidth(false));
                overrideRect.yMin += 4f;
                overrideRect.xMin += 8f;
                horizonMinMax.overrideState.boolValue = GUI.Toggle(overrideRect, horizonMinMax.overrideState.boolValue, EditorGUIUtility.TrTextContent("", "Override this setting for this volume."), CoreEditorStyles.smallTickbox);

                // Property
                using (new EditorGUI.DisabledScope(!horizonMinMax.overrideState.boolValue))
                {
                    EditorGUI.BeginChangeCheck();
                    
                    EditorGUILayout.LabelField(horizonMinMax.displayName, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                    EditorGUILayout.FloatField(minBrightness, GUILayout.Width(50f));
                    EditorGUILayout.MinMaxSlider(ref minBrightness, ref maxBrightness, 0f, 1f);
                    EditorGUILayout.FloatField(maxBrightness, GUILayout.Width(50f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        horizonMinMax.value.vector2Value = new Vector2(minBrightness, maxBrightness);
                    }
                }
            }

            //Always exclude skybox for skybox color mode
            PropertyField(skyboxInfluence, new GUIContent("Influence"));

            PropertyField(enableDirectionalLight);

            m_showSun.target = enableDirectionalLight.value.boolValue;
            if (EditorGUILayout.BeginFadeGroup(m_showSun.faded))
            {
                SCPE_GUI.DrawSunInfo();

                PropertyField(useLightDirection);
                if (!useLightDirection.value.boolValue) PropertyField(lightDirection);
                PropertyField(useLightColor);
                if (!useLightColor.value.boolValue) PropertyField(lightColor);
                PropertyField(useLightIntensity);
                if (!useLightIntensity.value.boolValue) PropertyField(lightIntensity);
                
                PropertyField(lightExponent);
            }
            EditorGUILayout.EndFadeGroup();

            PropertyField(heightFog);
            m_showHeight.target = heightFog.value.boolValue;
            if (EditorGUILayout.BeginFadeGroup(m_showHeight.faded))
            {
                PropertyField(height);
                PropertyField(heightDensity);

                PropertyField(heightFogNoise);
                if (heightFogNoise.value.boolValue)
                {
                    PropertyField(heightNoiseTex);
                    if (heightNoiseTex.value.objectReferenceValue)
                    {
                        PropertyField(heightNoiseSize);
                        PropertyField(heightNoiseStrength);
                        PropertyField(heightNoiseSpeed);
                    }

                }
            }
            EditorGUILayout.EndFadeGroup();

            /*
            PropertyField(lightScattering);
            m_showScattering.target = lightScattering.value.boolValue;
            if (EditorGUILayout.BeginFadeGroup(m_showScattering.faded))
            {
                PropertyField(scatterIntensity);
                PropertyField(scatterThreshold);
                PropertyField(scatterDiffusion);
                PropertyField(scatterSoftKnee);
            }
            EditorGUILayout.EndFadeGroup();
            */
        }
    }
}