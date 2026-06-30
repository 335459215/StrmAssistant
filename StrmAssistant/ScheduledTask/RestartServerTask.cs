using MediaBrowser.Controller;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using StrmAssistant.Properties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StrmAssistant.ScheduledTask
{
    public class RestartServerTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _serverApplicationHost;

        public RestartServerTask(IServerApplicationHost serverApplicationHost)
        {
            _logger = Plugin.Instance.Logger;
            _serverApplicationHost = serverApplicationHost;
        }

        public string Key => "RestartServerTask";

        public string Name => Resources.ResourceManager.GetString("RestartServerTask_Name_Restart_Server",
            Plugin.Instance.DefaultUICulture);
        //public string Name => "Restart Server";

        public string Description => Resources.ResourceManager.GetString(
            "RestartServerTask_Description_Restarts_the_Embymedia_server",
            Plugin.Instance.DefaultUICulture) ?? "Restarts the Emby media server";

        public string Category => Resources.ResourceManager.GetString("PluginOptions_EditorTitle_Strm_Assistant",
            Plugin.Instance.DefaultUICulture);

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // No automatic trigger - only manual execution
            return Array.Empty<TaskTriggerInfo>();
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("RestartServerTask - Initiating server restart");
            await Task.Delay(0).ConfigureAwait(false);
            progress.Report(50);

            try
            {
                _serverApplicationHost.NotifyPendingRestart();
                _logger.Info("RestartServerTask - Server restart notification sent");
            }
            catch (Exception e)
            {
                _logger.Error("RestartServerTask - Failed to restart: {0}", e.Message);
                _logger.Debug(e.StackTrace);
            }

            progress.Report(100);
        }

        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;
    }
}
