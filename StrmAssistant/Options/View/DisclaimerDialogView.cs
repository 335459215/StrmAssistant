using MediaBrowser.Model.Plugins;
using StrmAssistant.Options.UIBaseClasses.Views;
using System.Threading.Tasks;

namespace StrmAssistant.Options.View
{
    // [REMOVED] Disclaimer dialog view - content stripped
    internal class DisclaimerDialogView : PluginDialogView
    {
        public DisclaimerDialogView(PluginInfo pluginInfo) : base(pluginInfo.Id)
        {
        }

        public override string Caption => string.Empty;
    }
}
