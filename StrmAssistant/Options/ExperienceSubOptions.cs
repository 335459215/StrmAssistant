using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Properties;
using System.ComponentModel;

namespace StrmAssistant.Options.Experience
{
    public class MultipleVersionsOptions : EditableOptionsBase
    {
        [DisplayNameL("MultipleVersionsOptions_EditorTitle", typeof(Resources))]
        public override string EditorTitle => "Multiple Versions";

        [DisplayNameL("MultipleVersionsOptions_EnableAutoMerge", typeof(Resources))]
        [DescriptionL("MultipleVersionsOptions_Enable_automatic_version_merging", typeof(Resources))]
        [Required]
        public bool EnableAutoMerge { get; set; } = true;

        [DisplayNameL("MultipleVersionsOptions_MergeDelayMinutes", typeof(Resources))]
        [MinValue(0), MaxValue(1440)]
        [Required]
        public int MergeDelayMinutes { get; set; } = 5;
    }
}
