using UnityEditor;
using UnityEditor.Rendering;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(Transition))]
    sealed class TransitionEditor : VolumeComponentEditor
    {
        SerializedDataParameter gradientTex;
        SerializedDataParameter progress;
        SerializedDataParameter invert;
        SerializedDataParameter color;

        private bool isSetup;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<Transition>(serializedObject);
            isSetup = AutoSetup.ValidEffectSetup<TransitionRenderer>();

            gradientTex = Unpack(o.Find(x => x.gradientTex));
            progress = Unpack(o.Find(x => x.progress));
            invert = Unpack(o.Find(x => x.invert));
            color = Unpack(o.Find(x => x.color));
        }


        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("transition");

            SCPE_GUI.DisplaySetupWarning<TransitionRenderer>(ref isSetup);

            PropertyField(progress);
            SCPE_GUI.DisplayIntensityWarning(progress);
            
            EditorGUILayout.Space();
            
            PropertyField(gradientTex);
            SCPE_GUI.DisplayTextureOverrideWarning(gradientTex.overrideState.boolValue);

            if (gradientTex.overrideState.boolValue && gradientTex.value.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a gradient texture (pre-made textures can be found in the \"_Samples\" package", MessageType.Info);
            }
            
            PropertyField(invert);
            PropertyField(color);
        }
    }
}