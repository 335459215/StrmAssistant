using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
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
    public class ScanExternalTrackTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public ScanExternalTrackTask(IFileSystem fileSystem)
        {
            _logger = Plugin.Instance.Logger;
            _fileSystem = fileSystem;
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("ExternalTrack - Scheduled Task Execute");
            _logger.Info("Tier2 Max Concurrent Count: " +
                         Plugin.Instance.MainOptionsStore.GetOptions().GeneralOptions.Tier2MaxConcurrentCount);

            await Task.Delay(0).ConfigureAwait(false);
            progress.Report(0);

            var items = Plugin.LibraryApi.FetchPostExtractTaskItems(false);
            _logger.Info("ExternalTrack - Number of items: " + items.Count);

            double total = items.Count > 0 ? items.Count : 1;
            var index = 0;
            var current = 0;

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

                    var taskIndex = ++index;
                    var taskItem = item;
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            // Scan for external audio tracks (similar to subtitle scanning)
                            var mediaSources = Plugin.MediaInfoApi.GetStaticMediaSources(taskItem, false);
                            if (mediaSources == null) return;

                            foreach (var source in mediaSources)
                            {
                                if (cancellationToken.IsCancellationRequested) return;

                                var externalTracks = source.MediaStreams
                                    .Where(s => s.Type == MediaStreamType.Audio && s.IsExternal)
                                    .ToList();

                                if (externalTracks.Count > 0)
                                {
                                    _logger.Info("ExternalTrack - Found " + externalTracks.Count +
                                                 " external audio tracks for: " + taskItem.Name);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.Info("ExternalTrack - Item cancelled: " + taskItem.Name);
                        }
                        catch (Exception e)
                        {
                            _logger.Info("ExternalTrack - Item failed: " + taskItem.Name);
                            _logger.Debug(e.Message);
                            _logger.Debug(e.StackTrace);
                        }
                        finally
                        {
                            QueueManager.Tier2Semaphore.Release();

                            var currentCount = Interlocked.Increment(ref current);
                            progress.Report(currentCount / total * 100);
                            _logger.Info("ExternalTrack - Progress " + currentCount + "/" + total + " - " +
                                         "Task " + taskIndex + ": " + taskItem.Path);
                        }
                    });
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
                catch (OperationCanceledException) { /* expected on cancel */ }
                catch (Exception ex)
                {
                    _logger.Debug("ExternalTrack - Drain pending tasks: " + ex.Message);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Info("ExternalTrack - Scheduled Task Cancelled");
                }
                else
                {
                    progress.Report(100.0);
                    _logger.Info("ExternalTrack - Scheduled Task Complete");
                }
            }
            finally
            {
                tasks.Clear();
                items.Clear();
            }
        }

        public string Category => Resources.ResourceManager.GetString("PluginOptions_EditorTitle_Strm_Assistant",
            Plugin.Instance.DefaultUICulture);

        public string Key => "ScanExternalTrackTask";

        public string Description => Resources.ResourceManager.GetString(
            "ScanExternalTrackTask_Description_Scans_external_audio_tracks_for_videos",
            Plugin.Instance.DefaultUICulture) ?? "Scans external audio tracks for videos";

        public string Name => Resources.ResourceManager.GetString("ScanExternalTrackTask_Name_Scan_External_Tracks",
            Plugin.Instance.DefaultUICulture);
        //public string Name => "Scan External Tracks";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }

        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;
    }
}
