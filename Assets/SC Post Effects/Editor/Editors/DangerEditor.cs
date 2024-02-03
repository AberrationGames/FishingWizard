using UnityEditor;
using UnityEditor.Rendering;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(Danger))]
    sealed class DangerEditor : VolumeComponentEditor
    {
        SerializedDataParameter overlayTex;
        SerializedDataParameter color;
        SerializedDataParameter size;
        SerializedDataParameter intensity;

        private bool isSetup;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<Danger>(serializedObject);
            isSetup = AutoSetup.ValidEffectSetup<DangerRenderer>();

            overlayTex = Unpack(o.Find(x => x.overlayTex));
            color = Unpack(o.Find(x => x.color));
            intensity = Unpack(o.Find(x => x.intensity));
            size = Unpack(o.Find(x => x.size));
        }


        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("danger");

            SCPE_GUI.DisplaySetupWarning<DangerRenderer>(ref isSetup);

            PropertyField(intensity);
            SCPE_GUI.DisplayIntensityWarning(intensity);
            
            EditorGUILayout.Space();
            
            PropertyField(overlayTex);
            SCPE_GUI.DisplayTextureOverrideWarning(overlayTex.overrideState.boolValue);

            if (overlayTex.overrideState.boolValue && overlayTex.value.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a texture", MessageType.Info);
            }
            
            PropertyField(color);
            PropertyField(size);
        }
    }
}
