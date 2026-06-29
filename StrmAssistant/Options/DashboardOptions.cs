using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
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
}
