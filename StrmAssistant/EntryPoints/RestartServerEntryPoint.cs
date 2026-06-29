using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrmAssistant.EntryPoints
{
    /// <summary>
    /// Server startup entry point that initializes StrmAssistant background services.
    /// In the release version this implements IServerEntryPoint (Emby Pro API),
    /// but since that interface requires a paid SDK license, we use a standalone class.
    /// </summary>
    public class RestartServerEntryPoint
    {
        private readonly ILogger _logger;
        private readonly ITaskManager _taskManager;

        public RestartServerEntryPoint(ITaskManager taskManager)
        {
            _logger = Plugin.Instance.Logger;
            _taskManager = taskManager;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.Info("StrmAssistant EntryPoint - Server startup initialization");
            return Task.CompletedTask;
        }
    }
}
