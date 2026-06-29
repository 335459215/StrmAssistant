using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using StrmAssistant.Common;
using StrmAssistant.Dto.Douban;
using StrmAssistant.Properties;
using StrmAssistant.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StrmAssistant.ScheduledTask
{
    public class BuildDoubanCacheTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger _logger;

        public BuildDoubanCacheTask()
        {
            _logger = Plugin.Instance.Logger;
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("BuildDoubanCache - Scheduled Task Execute");

            await Task.Yield();
            progress.Report(0);

            var metadataEnhanceOptions = Plugin.Instance.MetadataEnhanceStore.GetOptions();
            if (!metadataEnhanceOptions.EnableDoubanAssistScraping)
            {
                _logger.Info("BuildDoubanCache - Douban Assist Scraping is disabled, skipping");
                progress.Report(100.0);
                return;
            }

            var items = Plugin.DoubanApi.FetchDoubanCacheItems();
            _logger.Info($"BuildDoubanCache - Number of items: {items.Count}");

            double total = items.Count;
            if (total == 0)
            {
                progress.Report(100.0);
                return;
            }

            var current = 0;
            var populated = 0;
            var failed = 0;
            var tasks = new List<Task>();

            try
            {
                foreach (var item in items)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        await QueueManager.Tier2Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        QueueManager.Tier2Semaphore.Release();
                        break;
                    }

                    var taskItem = item;
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            var doubanId = taskItem.GetProviderId(DoubanExternalId.StaticName);

                            if (string.IsNullOrWhiteSpace(doubanId))
                            {
                                doubanId = await Plugin.DoubanApi.FindDoubanId(taskItem, cancellationToken)
                                    .ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(doubanId))
                                {
                                    taskItem.SetProviderId(DoubanExternalId.StaticName, doubanId);
                                    Plugin.DoubanApi.UpdateItem(taskItem);
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(doubanId))
                            {
                                var detail = await Plugin.DoubanApi.GetDoubanDetail(doubanId, cancellationToken)
                                    .ConfigureAwait(false);

                                if (detail?.Rating != null && detail.Rating.Value > 0)
                                {
                                    taskItem.CommunityRating = (float)Math.Round(detail.Rating.Value, 1);
                                    Plugin.DoubanApi.UpdateItem(taskItem);
                                    Interlocked.Increment(ref populated);
                                }
                                else
                                {
                                    Interlocked.Increment(ref failed);
                                }
                            }
                            else
                            {
                                Interlocked.Increment(ref failed);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.Info($"BuildDoubanCache - Item cancelled: {taskItem.Name}");
                        }
                        catch (Exception e)
                        {
                            _logger.Error($"BuildDoubanCache - Item failed: {taskItem.Name}");
                            _logger.Error(e.Message);
                            _logger.Debug(e.StackTrace);
                            Interlocked.Increment(ref failed);
                        }
                        finally
                        {
                            QueueManager.Tier2Semaphore.Release();
                            var currentCount = Interlocked.Increment(ref current);
                            progress.Report(currentCount / total * 100);
                        }
                    }, cancellationToken);
                    tasks.Add(task);

                    if (tasks.Count >= 100)
                    {
                        tasks.RemoveAll(t => t.IsCompleted);
                    }

                    try
                    {
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.Debug("BuildDoubanCache - Drain pending tasks: " + ex.Message);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Info("BuildDoubanCache - Scheduled Task Cancelled");
                }
                else
                {
                    progress.Report(100.0);
                    _logger.Info($"BuildDoubanCache - Populated: {populated}, Failed: {failed}");
                    _logger.Info("BuildDoubanCache - Scheduled Task Complete");
                }
            }
            finally
            {
                tasks.Clear();
                items.Clear();
            }
        }

        public string Category => Resources.ResourceManager.GetString(
            "PluginOptions_EditorTitle_Strm_Assistant", Plugin.Instance.DefaultUICulture);

        public string Key => "BuildDoubanCacheTask";

        public string Description => Resources.ResourceManager.GetString(
            "BuildDoubanCacheTask_Description_Build_Douban_rating_cache", Plugin.Instance.DefaultUICulture);

        public string Name => Resources.ResourceManager.GetString("BuildDoubanCacheTask_Name_Build_Douban_Cache",
            Plugin.Instance.DefaultUICulture);
        //public string Name => "Build Douban Cache";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }

        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;
    }
}
