using UnityEditor;
using UnityEditor.Rendering;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(ColorSplit))]
    sealed class ColorSplitEditor : VolumeComponentEditor
    {
        SerializedDataParameter mode;
        SerializedDataParameter offset;
        SerializedDataParameter edgeMasking;
        SerializedDataParameter luminanceThreshold;

        private bool isSetup;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<ColorSplit>(serializedObject);
            isSetup = AutoSetup.ValidEffectSetup<ColorSplitRenderer>();

            mode = Unpack(o.Find(x => x.mode));
            offset = Unpack(o.Find(x => x.offset));
            edgeMasking = Unpack(o.Find(x => x.edgeMasking));
            luminanceThreshold = Unpack(o.Find(x => x.luminanceThreshold));
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("color-split");

            SCPE_GUI.DisplaySetupWarning<ColorSplitRenderer>(ref isSetup);

            PropertyField(offset);
            SCPE_GUI.DisplayIntensityWarning(offset);
            
            EditorGUILayout.Space();
            
            PropertyField(mode);
            PropertyField(edgeMasking);
            PropertyField(luminanceThreshold);
        }
    }
}
