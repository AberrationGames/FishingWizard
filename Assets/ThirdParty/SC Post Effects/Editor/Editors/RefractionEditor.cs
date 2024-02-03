using UnityEditor;
using UnityEditor.Rendering;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(Refraction))]
    sealed class RefractionEditor : VolumeComponentEditor
    {
        SerializedDataParameter normalMap;
        SerializedDataParameter amount;
        SerializedDataParameter tint;

        private bool isSetup;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<Refraction>(serializedObject);
            isSetup = AutoSetup.ValidEffectSetup<RefractionRenderer>();
            
            normalMap = Unpack(o.Find(x => x.normalMap));
            amount = Unpack(o.Find(x => x.amount));
            tint = Unpack(o.Find(x => x.tint));
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("refraction");

            SCPE_GUI.DisplaySetupWarning<RefractionRenderer>(ref isSetup);

            PropertyField(amount);
            SCPE_GUI.DisplayIntensityWarning(amount);
            
            EditorGUILayout.Space();
            
            PropertyField(normalMap);
            SCPE_GUI.DisplayTextureOverrideWarning(normalMap.overrideState.boolValue);

            if (normalMap.overrideState.boolValue && normalMap.value.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a texture", MessageType.Info);
            }

            PropertyField(tint);
        }
    }
}