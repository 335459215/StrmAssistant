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
    public class DashboardOptions : EditableOptionsBase
    {
        [DisplayNameL("DashboardOptions_EditorTitle_Dashboard", typeof(Resources))]
        public override string EditorTitle => "Dashboard";

        [DisplayNameL("DashboardOptions_ShowPluginStatus", typeof(Resources))]
        [DescriptionL("DashboardOptions_Show_plugin_status_on_dashboard", typeof(Resources))]
        public bool ShowPluginStatus { get; set; } = true;

        [DisplayNameL("DashboardOptions_ShowSystemInfo", typeof(Resources))]
        [DescriptionL("DashboardOptions_Show_system_info_on_dashboard", typeof(Resources))]
        public bool ShowSystemInfo { get; set; } = true;
    }

    public class MemoryOptions : EditableOptionsBase
    {
        [DisplayNameL("MemoryOptions_EditorTitle_Memory", typeof(Resources))]
        public override string EditorTitle => "Memory Management";

        [DisplayNameL("GeneralOptions_EnableMemoryCleanup_Title", typeof(Resources))]
        [DescriptionL("GeneralOptions_EnableMemoryCleanup_Description", typeof(Resources))]
        [Required]
        public bool EnableMemoryCleanup { get; set; } = true;

        [DisplayNameL("GeneralOptions_MemoryCleanupInterval_Title", typeof(Resources))]
        [DescriptionL("GeneralOptions_MemoryCleanupInterval_Description", typeof(Resources))]
        [MinValue(10)]
        [MaxValue(1440)]
        [Required]
        public int MemoryCleanupIntervalMinutes { get; set; } = 30;
    }
}
