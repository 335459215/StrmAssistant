using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Model.LocalizationAttributes;
using StrmAssistant.Options.Dashboard;
using StrmAssistant.Properties;
using System.ComponentModel;

namespace StrmAssistant.Options.Dashboard
{
    public class DashboardOptions : EditableOptionsBase
    {
        [DisplayNameL("DashboardOptions_EditorTitle_Dashboard", typeof(Resources))]
        public override string EditorTitle => "Dashboard";

        public override bool IsNewItem => false;

        public override bool FeatureRequiresPremiere => false;

        [DisplayNameL("DashboardOptions_ShowPluginStatus", typeof(Resources))]
        [DescriptionL("DashboardOptions_Show_plugin_status_on_dashboard", typeof(Resources))]
        public bool ShowPluginStatus { get; set; } = true;

        [DisplayNameL("DashboardOptions_ShowSystemInfo", typeof(Resources))]
        [DescriptionL("DashboardOptions_Show_system_info_on_dashboard", typeof(Resources))]
        public bool ShowSystemInfo { get; set; } = true;

        [IgnoreOnPersist]
        public GenericItemList SystemInfoItemList { get; set; } = new GenericItemList();

        [DisplayNameL("MemoryOptions_MemoryUsageLimitMb_Memory_Usage_Limit__MB_", typeof(Resources))]
        public MemoryOptions MemoryOptions { get; set; } = new MemoryOptions();
    }
}
