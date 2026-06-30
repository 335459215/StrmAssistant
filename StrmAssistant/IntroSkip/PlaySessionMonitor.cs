using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using StrmAssistant.Common;
using StrmAssistant.Mod;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static StrmAssistant.Options.Utility;

namespace StrmAssistant.IntroSkip
{
    public class PlaySessionMonitor
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;

        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(10);
        private readonly ConcurrentDictionary<string, PlaySessionData> _playSessionData = new ConcurrentDictionary<string, PlaySessionData>();
        private readonly ConcurrentDictionary<long, Task> _ongoingIntroUpdates = new ConcurrentDictionary<long, Task>();
        private readonly ConcurrentDictionary<long, Task> _ongoingCreditsUpdates = new ConcurrentDictionary<long, Task>();
        private readonly ConcurrentDictionary<long, DateTime> _lastIntroUpdateTimes = new ConcurrentDictionary<long, DateTime>();
        private readonly ConcurrentDictionary<long, DateTime> _lastCreditsUpdateTimes = new ConcurrentDictionary<long, DateTime>();
        private readonly object _introLock = new object();
        private readonly object _creditsLock = new object();

        private static Task _introSkipProcessTask;

        public static volatile IReadOnlyList<string> LibraryPathsInScope = Array.Empty<string>();
        public static volatile User[] UsersInScope = Array.Empty<User>();
        public static volatile IReadOnlySet<string> ClientsInScope = new HashSet<string>();

        public PlaySessionMonitor(ILibraryManager libraryManager, IUserManager userManager,
            ISessionManager sessionManager)
        {
            _logger = Plugin.Instance.Logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _sessionManager = sessionManager;

            UpdateLibraryPathsInScope(Plugin.Instance.IntroSkipStore.GetOptions().LibraryScope);
            UpdateUsersInScope(Plugin.Instance.IntroSkipStore.GetOptions().UserScope);
            UpdateClientInScope(Plugin.Instance.IntroSkipStore.GetOptions().ClientScope);
        }

        public void UpdateLibraryPathsInScope(string currentScope)
        {
            var validLibraryIds = GetValidLibraryIds(currentScope);

            var libraries = _libraryManager.GetVirtualFolders()
                .Where(f => (f.CollectionType == CollectionType.TvShows.ToString() || f.CollectionType is null) &&
                            (!validLibraryIds.Any() || validLibraryIds.Contains(f.Id)))
                .ToList();

            LibraryPathsInScope = libraries.SelectMany(l => l.Locations)
                .Select(ls => ls.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? ls
                    : ls + Path.DirectorySeparatorChar)
                .ToList().AsReadOnly();
        }

        public void UpdateLibraryPathsInScope()
        {
            UpdateLibraryPathsInScope(Plugin.Instance.IntroSkipStore.GetOptions().LibraryScope);
        }

        public void UpdateUsersInScope(string currentScope)
        {
            var userIds = Plugin.Instance.IntroSkipStore.GetOptions().UserScope
                ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(long.Parse).ToArray();

            var userQuery = new UserQuery
            {
                IsDisabled = false
            };

            if (userIds != null && userIds.Any())
            {
                userQuery.UserIds = userIds;
                UsersInScope = _userManager.GetUserList(userQuery);
            }
            else
            {
                UsersInScope = Array.Empty<User>();
            }
        }

        public void UpdateClientInScope(string currentScope)
        {
            ClientsInScope = new HashSet<string>(
                currentScope.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()), StringComparer.OrdinalIgnoreCase);
        }

        public void Initialize()
        {
            Dispose();

            _sessionManager.PlaybackStart += OnPlaybackStart;
            _sessionManager.PlaybackProgress += OnPlaybackProgress;
            _sessionManager.PlaybackStopped += OnPlaybackStopped;

            if (_introSkipProcessTask == null || _introSkipProcessTask.IsCompleted)
            {
                QueueManager.IntroSkipItemQueue.Clear();
                _introSkipProcessTask = QueueManager.IntroSkip_ProcessItemQueueAsync();
            }
        }

        private void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            if (!(e.Item is Episode episode) || !e.PlaybackPositionTicks.HasValue || !episode.IndexNumber.HasValue ||
                !(episode.ParentIndexNumber > 0))
            {
                return;
            }

            var options = Plugin.Instance.IntroSkipStore.GetOptions();

            _logger.Info("IntroSkip - Client Name: " + e.ClientName);
            _logger.Info("IntroSkip - Allowed Clients: " + options.ClientScope);

            var intoSkipLibraryScope = string.Join(", ",
                options.LibraryScope?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => options.LibraryList
                        .FirstOrDefault(option => option.Value == v)
                        ?.Name) ?? Enumerable.Empty<string>());
            _logger.Info("IntroSkip - LibraryScope is set to {0}",
                string.IsNullOrEmpty(intoSkipLibraryScope) ? "ALL" : intoSkipLibraryScope);

            var introSkipUserScope = string.Join(", ",
                options.UserScope?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => options.UserList
                        .FirstOrDefault(option => option.Value == v)
                        ?.Name) ?? Enumerable.Empty<string>());
            _logger.Info("IntroSkip - UserScope is set to {0}",
                string.IsNullOrEmpty(introSkipUserScope) ? "ALL" : introSkipUserScope);

            _playSessionData.TryRemove(e.PlaySessionId, out _);
            var playSessionData = GetPlaySessionData(e);
            if (playSessionData is null)
            {
                _logger.Info("IntroSkip - No detection as not in scope");
                return;
            }

            playSessionData.PlaybackStartTicks = e.PlaybackPositionTicks.Value;
            playSessionData.PreviousPositionTicks = e.PlaybackPositionTicks.Value;
            playSessionData.PreviousEventTime = DateTime.UtcNow;

            if (playSessionData.NoDetectionButReset)
            {
                _logger.Info("IntroSkip - No detection but allows pause to reset");
                return;
            }

            if (playSessionData.IntroStart.HasValue && playSessionData.CreditsStart.HasValue)
            {
                _logger.Info("IntroSkip - Intro marker and Credits marker already exist");
            }
            else
            {
                _logger.Info("Playback start time: " +
                             new TimeSpan(playSessionData.PlaybackStartTicks).ToString(@"hh\:mm\:ss\.fff"));
                _logger.Info("IntroSkip - Detection Started");
                _logger.Info("IntroSkip - Intro marker is " +
                             (playSessionData.IntroStart.HasValue ? "available" : "not available"));
                _logger.Info("IntroSkip - Credits marker is " +
                             (playSessionData.CreditsStart.HasValue ? "available" : "not available"));
            }
        }

        private void OnPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            //_logger.Debug("IntroSkip - EventName: " + e.EventName);

            if (!(e.Item is Episode episode) || e.EventName != ProgressEvent.TimeUpdate && e.EventName != ProgressEvent.Unpause &&
                e.EventName != ProgressEvent.PlaybackRateChange && e.EventName != ProgressEvent.Pause ||
                !e.PlaybackPositionTicks.HasValue || e.PlaybackPositionTicks.Value == 0)
                return;

            var playSessionData = GetPlaySessionData(e);
            if (playSessionData is null) return;

            var currentPositionTicks = e.PlaybackPositionTicks.Value;
            var currentEventTime = DateTime.UtcNow;
            var introStart = playSessionData.IntroStart;
            var introEnd = playSessionData.IntroEnd;
            var creditsStart = playSessionData.CreditsStart;

            if (!playSessionData.NoDetectionButReset && e.EventName == ProgressEvent.TimeUpdate && !introEnd.HasValue)
            {
                var elapsedTime = (currentEventTime - playSessionData.PreviousEventTime).TotalSeconds;
                var positionTimeDiff = TimeSpan.FromTicks(currentPositionTicks - playSessionData.PreviousPositionTicks)
                    .TotalSeconds;

                if (Math.Abs(positionTimeDiff) - elapsedTime > 5 &&
                    currentPositionTicks < playSessionData.MaxIntroDurationTicks)
                {
                    _logger.Info(
                        positionTimeDiff > 0
                            ? "Fast-forward {0} seconds by {1}."
                            : "Rewind {0} seconds by {1}.", positionTimeDiff - elapsedTime, e.Session.UserName);

                    if (!playSessionData.FirstJumpPositionTicks.HasValue &&
                        TimeSpan.FromTicks(playSessionData.PlaybackStartTicks).TotalSeconds < 5 &&
                        positionTimeDiff > 0) //fast-forward only
                    {
                        playSessionData.FirstJumpPositionTicks = playSessionData.PreviousPositionTicks;
                        if (playSessionData.PreviousPositionTicks > playSessionData.MinOpeningPlotDurationTicks)
                        {
                            _logger.Info("First jump start time: " +
                                         new TimeSpan(playSessionData.FirstJumpPositionTicks.Value).ToString(
                                             @"hh\:mm\:ss\.fff"));
                            playSessionData.MaxIntroDurationTicks += playSessionData.PreviousPositionTicks;
                            _logger.Info("MaxIntroDurationSeconds is extended to: {0} ({1})",
                                TimeSpan.FromTicks(playSessionData.MaxIntroDurationTicks).TotalSeconds
                                , TimeSpan.FromTicks(playSessionData.MaxIntroDurationTicks).ToString(@"hh\:mm\:ss\.fff"));
                        }
                    }

                    playSessionData.LastJumpPositionTicks = currentPositionTicks;
                    _logger.Info("Last jump to time: " +
                                 new TimeSpan(playSessionData.LastJumpPositionTicks.Value).ToString(@"hh\:mm\:ss\.fff"));
                }

                if (currentPositionTicks >= playSessionData.MaxIntroDurationTicks)
                {
                    if (playSessionData.LastJumpPositionTicks.HasValue)
                    {
                        UpdateIntroTask(episode, e.Session, playSessionData,
                            playSessionData.FirstJumpPositionTicks.HasValue &&
                            playSessionData.FirstJumpPositionTicks.Value > playSessionData.MinOpeningPlotDurationTicks
                                ? playSessionData.FirstJumpPositionTicks.Value
                                : new TimeSpan(0, 0, 0).Ticks,
                            playSessionData.LastJumpPositionTicks.Value);
                    }
                }

                playSessionData.PreviousPositionTicks = currentPositionTicks;
                playSessionData.PreviousEventTime = currentEventTime;
            }

            if (e.EventName == ProgressEvent.Pause)
            {
                playSessionData.LastPauseEventTime = currentEventTime;
                return;
            }

            if (e.EventName == ProgressEvent.PlaybackRateChange)
            {
                playSessionData.LastPlaybackRateChangeEventTime = currentEventTime;
                return;
            }

            if (e.EventName == ProgressEvent.Unpause && playSessionData.LastPauseEventTime.HasValue &&
                (currentEventTime - playSessionData.LastPauseEventTime.Value).TotalMilliseconds <
                (playSessionData.LastPlaybackRateChangeEventTime.HasValue ? 1500 : 500))
            {
                playSessionData.LastPauseEventTime = null;
                return;
            }

            if (!playSessionData.NoDetectionButReset && e.EventName == ProgressEvent.Unpause &&
                playSessionData.LastPauseEventTime.HasValue &&
                (currentEventTime - playSessionData.LastPauseEventTime.Value).TotalMilliseconds > 500 &&
                (currentEventTime - playSessionData.LastPauseEventTime.Value).TotalMilliseconds < 5000 &&
                introStart.HasValue && introStart.Value < currentPositionTicks && introEnd.HasValue &&
                currentPositionTicks < Math.Max(playSessionData.MaxIntroDurationTicks, introEnd.Value) &&
                Math.Abs(TimeSpan.FromTicks(currentPositionTicks - introEnd.Value).TotalMilliseconds) >
                (playSessionData.LastPlaybackRateChangeEventTime.HasValue ? 500 : 0))
            {
                UpdateIntroTask(episode, e.Session, playSessionData, introStart.Value, currentPositionTicks);
            }

            if (playSessionData.NoDetectionButReset && e.EventName == ProgressEvent.Unpause &&
                playSessionData.LastPauseEventTime.HasValue &&
                (currentEventTime - playSessionData.LastPauseEventTime.Value).TotalMilliseconds > 500 &&
                (currentEventTime - playSessionData.LastPauseEventTime.Value).TotalMilliseconds < 5000 &&
                currentPositionTicks < playSessionData.MaxIntroDurationTicks &&
                (!introEnd.HasValue ||
                 Math.Abs(TimeSpan.FromTicks(currentPositionTicks - introEnd.Value).TotalMilliseconds) >
                 (playSessionData.LastPlaybackRateChangeEventTime.HasValue ? 500 : 0)))
            {
                UpdateIntroTask(episode, e.Session, playSessionData, new TimeSpan(0, 0, 0).Ticks,
                    currentPositionTicks);
            }

            if (e.EventName == ProgressEvent.Unpause && episode.RunTimeTicks.HasValue &&
                playSessionData.LastPauseEventTime.HasValue &&
                (currentEventTime - playSessionData.LastPauseEventTime.Value).TotalMilliseconds > 500 &&
                (currentEventTime - playSessionData.LastPauseEventTime.Value).TotalMilliseconds < 5000 &&
                currentPositionTicks > episode.RunTimeTicks - playSessionData.MaxCreditsDurationTicks &&
                (creditsStart.HasValue || playSessionData.NoDetectionButReset))
            {
                if (episode.RunTimeTicks.Value > currentPositionTicks)
                {
                    UpdateCreditsTask(episode, e.Session, playSessionData,
                        episode.RunTimeTicks.Value - currentPositionTicks);
                }
            }
        }

        private void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            if (!(e.Item is Episode episode) || !e.PlaybackPositionTicks.HasValue || !episode.RunTimeTicks.HasValue)
            {
                _playSessionData.TryRemove(e.PlaySessionId, out _);
                return;
            }

            var playSessionData = GetPlaySessionData(e);
            if (playSessionData != null && !playSessionData.CreditsStart.HasValue)
            {
                var currentPositionTicks = e.PlaybackPositionTicks.Value;
                if (currentPositionTicks > episode.RunTimeTicks - playSessionData.MaxCreditsDurationTicks)
                {
                    if (episode.RunTimeTicks.Value > currentPositionTicks)
                    {
                        UpdateCreditsTask(episode, e.Session, playSessionData,
                            episode.RunTimeTicks.Value - currentPositionTicks);
                    }
                }
            }

            _playSessionData.TryRemove(e.PlaySessionId, out _);
            _lastIntroUpdateTimes.TryRemove(episode.InternalId, out _);
            _lastCreditsUpdateTimes.TryRemove(episode.InternalId, out _);
        }

        private PlaySessionData GetPlaySessionData(PlaybackProgressEventArgs e)
        {
            if (!IsLibraryInScope(e.Item) || !IsUserInScope(e.Session.UserInternalId) ||
                !IsClientInScope(e.ClientName)) return null;

            return _playSessionData.GetOrAdd(e.PlaySessionId, _ => new PlaySessionData(e.Item));
        }

        public bool IsLibraryInScope(BaseItem item)
        {
            var paths = LibraryPathsInScope; // 捕获 volatile 引用
            return !string.IsNullOrEmpty(item.Path) && paths.Any(l => item.Path.StartsWith(l));
        }

        public bool IsUserInScope(long userInternalId)
        {
            var users = UsersInScope; // 捕获 volatile 引用
            if (!users.Any())
                return true;

            var isUserInScope = users.Any(u => u.InternalId == userInternalId);

            return isUserInScope;
        }

        public bool IsClientInScope(string clientName)
        {
            var clients = ClientsInScope; // 捕获 volatile 引用
            return clients.Any(c => clientName.Contains(c, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateIntroTask(Episode episode, SessionInfo session, PlaySessionData playSessionData,
            long introStartPositionTicks,
            long introEndPositionTicks)
        {
            var now = DateTime.UtcNow;
            var episodeId = episode.InternalId;

            lock (_introLock)
            {
                if (_ongoingIntroUpdates.ContainsKey(episodeId))
                {
                    return;
                }

                if (_lastIntroUpdateTimes.TryGetValue(episodeId, out var lastUpdateTime))
                {
                    if (now - lastUpdateTime < _updateInterval)
                    {
                        return;
                    }
                }

                // Pre-register to prevent races, then launch the task
                var tcs = new TaskCompletionSource<bool>();
                _ongoingIntroUpdates[episodeId] = tcs.Task;
                _lastIntroUpdateTimes[episodeId] = now;

                Task.Run(() =>
                {
                    try
                    {
                        Plugin.ChapterApi.UpdateIntro(episode, session, introStartPositionTicks,
                            introEndPositionTicks);
                        playSessionData.IntroStart = Plugin.ChapterApi.GetIntroStart(episode);
                        playSessionData.IntroEnd = Plugin.ChapterApi.GetIntroEnd(episode);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Error updating intro marker: {0}", e.Message);
                        _logger.Debug(e.StackTrace);
                    }
                    finally
                    {
                        tcs.TrySetResult(true);
                        _ongoingIntroUpdates.TryRemove(episodeId, out _);
                    }
                }).ContinueWith(t => { if (t.IsFaulted) ThreadLogHelper.Log("Error", $"UpdateIntroTask unobserved fault: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}"); }, TaskScheduler.Default);
            }
        }

        private void UpdateCreditsTask(Episode episode, SessionInfo session, PlaySessionData playSessionData,
            long creditsDurationTicks)
        {
            var now = DateTime.UtcNow;
            var episodeId = episode.InternalId;

            lock (_creditsLock)
            {
                if (_ongoingCreditsUpdates.ContainsKey(episodeId))
                {
                    return;
                }

                if (_lastCreditsUpdateTimes.TryGetValue(episodeId, out var lastUpdateTime))
                {
                    if (now - lastUpdateTime < _updateInterval)
                    {
                        return;
                    }
                }

                // Pre-register to prevent races, then launch the task
                var tcs = new TaskCompletionSource<bool>();
                _ongoingCreditsUpdates[episodeId] = tcs.Task;
                _lastCreditsUpdateTimes[episodeId] = now;

                Task.Run(() =>
                {
                    try
                    {
                        Plugin.ChapterApi.UpdateCredits(episode, session, creditsDurationTicks);
                        playSessionData.CreditsStart = Plugin.ChapterApi.GetCreditsStart(episode);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Error updating credits marker: {0}", e.Message);
                        _logger.Debug(e.StackTrace);
                    }
                    finally
                    {
                        tcs.TrySetResult(true);
                        _ongoingCreditsUpdates.TryRemove(episodeId, out _);
                    }
                }).ContinueWith(t => { if (t.IsFaulted) ThreadLogHelper.Log("Error", $"UpdateCreditsTask unobserved fault: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}"); }, TaskScheduler.Default);
            }
        }

        public void Dispose()
        {
            _sessionManager.PlaybackStart -= OnPlaybackStart;
            _sessionManager.PlaybackProgress -= OnPlaybackProgress;
            _sessionManager.PlaybackStopped -= OnPlaybackStopped;

            // Wait for ongoing update tasks to complete before clearing
            try
            {
                var introTasks = _ongoingIntroUpdates.Values.ToArray();
                var creditsTasks = _ongoingCreditsUpdates.Values.ToArray();
                var allTasks = introTasks.Concat(creditsTasks).Where(t => !t.IsCompleted).ToArray();
                if (allTasks.Length > 0)
                {
                    Task.WaitAll(allTasks, TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception e)
            {
                _logger.Debug("Error waiting for pending tasks during Dispose: {0}", e.Message);
            }

            _playSessionData.Clear();
            _ongoingIntroUpdates.Clear();
            _ongoingCreditsUpdates.Clear();
            _lastIntroUpdateTimes.Clear();
            _lastCreditsUpdateTimes.Clear();

            // Best-effort cancel — QueueManager.Dispose() will also clean this up.
            // Guard against ObjectDisposedException if QueueManager already disposed it.
            try { QueueManager.IntroSkipTokenSource?.Cancel(); }
            catch (ObjectDisposedException) { /* already disposed by QueueManager */ }
        }
    }
}
