using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Properties;
using System.ComponentModel;

namespace StrmAssistant.Options.Metadata
{
    public class MetadataBuildOptions : EditableOptionsBase
    {
        [DisplayNameL("MetadataBuildOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Metadata Build";

        [DisplayNameL("MetadataBuildOptions_ReplaceExistingMetadata", typeof(Resources))]
        [DescriptionL("MetadataBuildOptions_Replace_existing_metadata_during_build", typeof(Resources))]
        [Required]
        public bool ReplaceExistingMetadata { get; set; } = false;

        [DisplayNameL("MetadataBuildOptions_SkipItemsWithExistingMetadata", typeof(Resources))]
        [DescriptionL("MetadataBuildOptions_Skip_items_that_already_have_metadata", typeof(Resources))]
        [Required]
        public bool SkipItemsWithExistingMetadata { get; set; } = true;
    }

    public class MetadataRefreshOptions : EditableOptionsBase
    {
        [DisplayNameL("MetadataRefreshOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Metadata Refresh";

        [DisplayNameL("MetadataRefreshOptions_EnableAutoRefresh", typeof(Resources))]
        [DescriptionL("MetadataRefreshOptions_Enable_automatic_metadata_refresh", typeof(Resources))]
        [Required]
        public bool EnableAutoRefresh { get; set; } = true;

        [DisplayNameL("MetadataRefreshOptions_RefreshInterval", typeof(Resources))]
        [MinValue(1), MaxValue(365)]
        [Required]
        public int RefreshIntervalDays { get; set; } = 30;
    }

    public class MetadataScrapeOptions : EditableOptionsBase
    {
        [DisplayNameL("MetadataScrapeOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Metadata Scrape";

        [DisplayNameL("MetadataScrapeOptions_EnableChineseSearch", typeof(Resources))]
        [DescriptionL("MetadataScrapeOptions_Enable_Chinese_metadata_search", typeof(Resources))]
        [Required]
        public bool EnableChineseSearch { get; set; } = true;

        [DisplayNameL("MetadataScrapeOptions_EnableDoubanSearch", typeof(Resources))]
        [DescriptionL("MetadataScrapeOptions_Enable_Douban_metadata_search", typeof(Resources))]
        [Required]
        public bool EnableDoubanSearch { get; set; } = false;
    }
}
