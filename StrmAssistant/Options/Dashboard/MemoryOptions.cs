using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Properties;
using System.ComponentModel;

namespace StrmAssistant.Options.Dashboard
{
    public class MemoryOptions : EditableOptionsBase
    {
        public override string EditorTitle => string.Empty;

        public override bool IsNewItem => false;

        public override bool FeatureRequiresPremiere => false;

        [DisplayNameL("MemoryOptions_MemoryUsageLimitMb_Memory_Usage_Limit__MB_", typeof(Resources))]
        [DescriptionL("MemoryOptions_MemoryUsageLimitMB_Restart_server_if_memory_exceeds_limit_for_15_minutes_during_idle__Leave_blank_to_disable_", typeof(Resources))]
        [MinValue(500)]
        public int? MemoryUsageLimitMB { get; set; }
    }
}
