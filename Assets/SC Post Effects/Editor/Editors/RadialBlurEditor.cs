using UnityEditor;
using UnityEditor.Rendering;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(RadialBlur))]
    sealed class RadialBlurEditor : VolumeComponentEditor
    {
        SerializedDataParameter amount;
        SerializedDataParameter center;
        SerializedDataParameter angle;
        SerializedDataParameter iterations;

        private bool isSetup;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<RadialBlur>(serializedObject);
            isSetup = AutoSetup.ValidEffectSetup<RadialBlurRenderer>();

            amount = Unpack(o.Find(x => x.amount));
            center = Unpack(o.Find(x => x.center));
            angle = Unpack(o.Find(x => x.angle));
            iterations = Unpack(o.Find(x => x.iterations));
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("radial-blur");

            SCPE_GUI.DisplaySetupWarning<RadialBlurRenderer>(ref isSetup);

            PropertyField(amount);
            SCPE_GUI.DisplayIntensityWarning(amount);
            PropertyField(center);
            PropertyField(angle);
            PropertyField(iterations);
        }
    }
}