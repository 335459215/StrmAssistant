using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using StrmAssistant.Common;
using StrmAssistant.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StrmAssistant.ScheduledTask
{
    public class TriggerPopulateDoubanIdTask : ILibraryPostScanTask
    {
        private readonly ILogger _logger;
        private readonly ITaskManager _taskManager;

        public TriggerPopulateDoubanIdTask(ITaskManager taskManager)
        {
            _logger = Plugin.Instance.Logger;
            _taskManager = taskManager;
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.Info("TriggerPopulateDoubanId - Library post-scan task triggered");

            var metadataEnhanceOptions = Plugin.Instance.MetadataEnhanceStore.GetOptions();
            if (!metadataEnhanceOptions.EnableDoubanAssistScraping)
            {
                _logger.Info("TriggerPopulateDoubanId - Douban Assist Scraping is disabled, skipping");
                progress.Report(100.0);
                return;
            }

            var buildTask = _taskManager.ScheduledTasks.FirstOrDefault(t =>
                t.ScheduledTask is BuildDoubanCacheTask);

            if (buildTask != null && buildTask.State == TaskState.Running)
            {
                _logger.Info("TriggerPopulateDoubanId - BuildDoubanCacheTask is already running, skipping");
                progress.Report(100.0);
                return;
            }

            if (buildTask != null)
            {
                try
                {
                    _ = _taskManager.Execute(buildTask, new TaskOptions());
                    _logger.Info("TriggerPopulateDoubanId - BuildDoubanCacheTask triggered successfully");
                }
                catch (Exception ex)
                {
                    _logger.Error("TriggerPopulateDoubanId - Failed to trigger BuildDoubanCacheTask");
                    _logger.Error(ex.Message);
                }
            }

            progress.Report(100.0);
            await Task.CompletedTask;
        }
    }
}
