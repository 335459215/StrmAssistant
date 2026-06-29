using Emby.Web.GenericEdit;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Properties;

namespace StrmAssistant.Options.IntroSkip
{
    public class IntroSkipFingerprintOptions : EditableOptionsBase
    {
        [DisplayNameL("IntroSkipFingerprintOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => Resources.IntroSkipFingerprintOptions_EditorTitle;

        public override bool IsNewItem => false;

        public override bool FeatureRequiresPremiere => false;
    }
}
