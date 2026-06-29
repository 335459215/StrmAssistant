using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Properties;
using System.ComponentModel;

namespace StrmAssistant.Options.Activation
{
    public class ActivationUI : EditableOptionsBase
    {
        [DisplayNameL("ActivationUI_EditorTitle_Activation", typeof(Resources))]
        public override string EditorTitle => "Activation";

        [DisplayNameL("ActivationUI_LicenseKey", typeof(Resources))]
        [DescriptionL("ActivationUI_Enter_your_license_key_to_unlock_pro_features", typeof(Resources))]
        public string LicenseKey { get; set; } = string.Empty;

        [Browsable(false)]
        public bool IsActivated => !string.IsNullOrWhiteSpace(LicenseKey);

        [Browsable(false)]
        public GenericItemList ActivationResult { get; set; } = new GenericItemList();

        public SpacerItem ActivationResultSeparator { get; set; } = new SpacerItem(SpacerSize.Small);

        public ButtonItem ValidateButton =>
            new ButtonItem("Validate License")
            {
                Icon = IconNames.check_circle, Data1 = "ValidateLicense"
            };
    }

    public class CreditsUI : EditableOptionsBase
    {
        [DisplayNameL("CreditsUI_EditorTitle_Credits", typeof(Resources))]
        public override string EditorTitle => "Credits";

        [DisplayNameL("CreditsUI_Acknowledgements", typeof(Resources))]
        [DescriptionL("CreditsUI_Open_source_projects_and_contributors", typeof(Resources))]
        [ReadOnly(true)]
        public string Acknowledgements { get; set; } =
            "StrmAssistant uses the following open-source projects:\n" +
            "- LibVLCSharp\n- HarmonyX\n- TinyPinyin\n- ChineseConverter\n- Newtonsoft.Json";
    }
}
