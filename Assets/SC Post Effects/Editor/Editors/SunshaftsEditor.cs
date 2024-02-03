using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(Sunshafts))]
    sealed class SunshaftsEditor : VolumeComponentEditor
    {
        Sunshafts effect;

        SerializedDataParameter useCasterColor;
        SerializedDataParameter useCasterIntensity;

        SerializedDataParameter resolution;
        SerializedDataParameter sunThreshold;
        SerializedDataParameter blendMode;
        SerializedDataParameter sunColor;
        SerializedDataParameter sunShaftIntensity;
        SerializedDataParameter falloff;

        SerializedDataParameter length;
        SerializedDataParameter highQuality;

        private bool isSetup;

        public override void OnEnable()
        {
            base.OnEnable();

            effect = (Sunshafts)target;
            var o = new PropertyFetcher<Sunshafts>(serializedObject);

            isSetup = AutoSetup.ValidEffectSetup<SunshaftsRenderer>();

            useCasterColor = Unpack(o.Find(x => x.useCasterColor));
            useCasterIntensity = Unpack(o.Find(x => x.useCasterIntensity));

            resolution = Unpack(o.Find(x => x.resolution));
            sunThreshold = Unpack(o.Find(x => x.sunThreshold));
            blendMode = Unpack(o.Find(x => x.blendMode));
            sunColor = Unpack(o.Find(x => x.sunColor));
            sunShaftIntensity = Unpack(o.Find(x => x.sunShaftIntensity));
            falloff = Unpack(o.Find(x => x.falloff));

            length = Unpack(o.Find(x => x.length));
            highQuality = Unpack(o.Find(x => x.highQuality));
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("sunshafts");

            SCPE_GUI.ShowDepthTextureWarning();
            
            SCPE_GUI.DrawSunInfo();

            SCPE_GUI.DisplaySetupWarning<SunshaftsRenderer>(ref isSetup, false);

            if (useCasterIntensity.value.boolValue == false)
            {
                PropertyField(sunShaftIntensity);
                SCPE_GUI.DisplayIntensityWarning(sunShaftIntensity);
            }
            
            EditorGUILayout.LabelField("Quality");
            PropertyField(resolution);
            PropertyField(highQuality, new GUIContent("High quality"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Use values from caster");
            PropertyField(useCasterColor, new GUIContent("Color"));
            if (useCasterColor.value.boolValue == false)
            {
                EditorGUI.indentLevel++;
                PropertyField(sunColor);
                EditorGUI.indentLevel--;
            }
            PropertyField(useCasterIntensity, new GUIContent("Intensity"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sunshafts");
            PropertyField(blendMode);
            if (blendMode.value.intValue == 1) EditorGUILayout.HelpBox("Screen blend mode currently not supported in URP", MessageType.Warning);
            PropertyField(sunThreshold);
            PropertyField(falloff);
            PropertyField(length);
            
        }

    }
}