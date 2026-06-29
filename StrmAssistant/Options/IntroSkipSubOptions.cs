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
    public class IntroSkipFingerprintOptions : EditableOptionsBase
    {
        [DisplayNameL("IntroSkipFingerprintOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Fingerprint Detection";

        [DisplayNameL("IntroSkipOptions_IntroDetectionFingerprintMinutes_Intro_Detection_Fingerprint_Minutes", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_IntroDetectionFingerprintMinutes_It_must_be_between_2_and_20__Default_is_10_", typeof(Resources))]
        [MinValue(2), MaxValue(20)]
        [Required]
        public int IntroDetectionFingerprintMinutes { get; set; } = 10;

        [DisplayNameL("IntroSkipOptions_MaxIntroDurationSeconds", typeof(Resources))]
        [MinValue(10), MaxValue(600)]
        [Required]
        public int MaxIntroDurationSeconds { get; set; } = 150;

        [DisplayNameL("IntroSkipOptions_MaxCreditsDurationSeconds", typeof(Resources))]
        [MinValue(10), MaxValue(600)]
        [Required]
        public int MaxCreditsDurationSeconds { get; set; } = 360;

        [DisplayNameL("IntroSkipOptions_MinOpeningPlotDurationSeconds", typeof(Resources))]
        [MinValue(30), MaxValue(120)]
        [Required]
        public int MinOpeningPlotDurationSeconds { get; set; } = 60;
    }

    public class IntroSkipPlaybackOptions : EditableOptionsBase
    {
        [DisplayNameL("IntroSkipPlaybackOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Playback Settings";

        [DisplayNameL("IntroSkipOptions_ClientScope_Client_Scope", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_ClientScope_Allowed_clients__Default_is_EMPTY", typeof(Resources))]
        public string ClientScope { get; set; } = string.Empty;

        [DisplayNameL("IntroSkipOptions_UserScope_User_Scope", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_UserScope_Users_allowed_to_detect__Blank_includes_all", typeof(Resources))]
        public string UserScope { get; set; } = string.Empty;

        [DisplayNameL("IntroSkipOptions_MaxAutoSkipIntroDurationSeconds", typeof(Resources))]
        [MinValue(5), MaxValue(300)]
        [Required]
        public int MaxAutoSkipIntroDurationSeconds { get; set; } = 120;
    }

    public class IntroSkipScrapeOptions : EditableOptionsBase
    {
        [DisplayNameL("IntroSkipScrapeOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Scrape Settings";

        [DisplayNameL("IntroSkipOptions_BlacklistShows_Optional_Blacklist_Shows", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_BlacklistShows_List_of_Series_Id_or_Season_Id_separated_by_comma_or_semicolon__Default_is_EMPTY", typeof(Resources))]
        public string FingerprintBlacklistShows { get; set; } = string.Empty;

        [DisplayNameL("IntroSkipOptions_MarkerEnabledLibraryScope_Library_Scope", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_MarkerEnabledLibraryScope_Intro_detection_enabled_library_scope__Blank_includes_all_", typeof(Resources))]
        public string MarkerEnabledLibraryScope { get; set; } = string.Empty;
    }
}
