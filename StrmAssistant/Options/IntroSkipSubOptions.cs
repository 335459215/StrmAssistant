using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Properties;
using System.ComponentModel;

namespace StrmAssistant.Options.IntroSkip
{
    public class IntroSkipPlaybackOptions : EditableOptionsBase
    {
        [DisplayNameL("IntroSkipPlaybackOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Playback Settings";

        [DisplayNameL("IntroSkipOptions_MaxAutoSkipIntroDurationSeconds", typeof(Resources))]
        [MinValue(5), MaxValue(300)]
        [Required]
        public int MaxAutoSkipIntroDurationSeconds { get; set; } = 120;

        // ClientScope and UserScope remain in IntroSkipOptions (main class)
    }
}
