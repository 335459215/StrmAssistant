using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace StrmAssistant.Options.IntroSkip
{
    public class IntroSkipScrapeOptions : EditableOptionsBase
    {
        public enum IntroDbProvider
        {
            IntroDb,
            Tidb
        }

        [DisplayNameL("IntroSkipScrapeOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => Resources.IntroSkipScrapeOptions_EditorTitle;

        public override bool IsNewItem => false;

        public override bool FeatureRequiresPremiere => false;

        [Browsable(false)]
        public List<EditorSelectOption> UnifiedIntroDbProvidersList { get; set; } = new List<EditorSelectOption>();

        [DisplayNameL("IntroSkipOptions_UnifiedIntroDbProviders_Unified_IntroDb_Providers", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_UnifiedIntroDbProviders_Tried_in_order_until_one_succeeds__Default_is_IntroDb_Tidb", typeof(Resources))]
        [EditMultilSelect]
        [SelectItemsSource(nameof(UnifiedIntroDbProvidersList))]
        public string UnifiedIntroDbProviders { get; set; } = "IntroDb,Tidb";

        [DisplayNameL("IntroSkipOptions_IntroDbApiKey_IntroDb_API_Key", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_IntroDbApiKey_Optional_API_key_for_higher_daily_usage_limits", typeof(Resources))]
        public string IntroDbApiKey { get; set; } = string.Empty;

        [DisplayNameL("IntroSkipOptions_TidbApiKey_TIDB_API_Key", typeof(Resources))]
        [DescriptionL("IntroSkipOptions_TidbApiKey_API_key_increases_daily_usage_limits", typeof(Resources))]
        public string TidbApiKey { get; set; } = string.Empty;

        public void Initialize()
        {
            UnifiedIntroDbProvidersList.Clear();
            foreach (IntroDbProvider provider in Enum.GetValues(typeof(IntroDbProvider)))
            {
                UnifiedIntroDbProvidersList.Add(new EditorSelectOption
                {
                    Value = provider.ToString(),
                    Name = provider.ToString(),
                    IsEnabled = true
                });
            }
        }
    }
}
