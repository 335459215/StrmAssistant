using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using StrmAssistant.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace StrmAssistant.ScheduledTask
{
    public class UpdatePluginTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger _logger;
        private readonly IApplicationHost _applicationHost;
        private readonly IApplicationPaths _applicationPaths;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IServerApplicationHost _serverApplicationHost;

        public UpdatePluginTask(IApplicationHost applicationHost, IApplicationPaths applicationPaths,
            IHttpClient httpClient, IJsonSerializer jsonSerializer, IActivityManager activityManager,
            ILocalizationManager localizationManager, IServerApplicationHost serverApplicationHost)
        {
            _logger = Plugin.Instance.Logger;
            _applicationHost = applicationHost;
            _applicationPaths = applicationPaths;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _activityManager = activityManager;
            _localizationManager = localizationManager;
            _serverApplicationHost = serverApplicationHost;
        }

        private static string PluginAssemblyFilename => Assembly.GetExecutingAssembly().GetName().Name + ".dll";
        private static string RepoReleaseUrl => "https://api.github.com/repos/sjtuross/StrmAssistant/releases/latest";

        public string Key => "UpdatePluginTask";

        public string Name =>
            Resources.ResourceManager.GetString("UpdatePluginTask_Name_Update_Plugin",
                Plugin.Instance.DefaultUICulture);
        //public string Name => "Update Plugin";

        public string Description => Resources.ResourceManager.GetString(
            "UpdatePluginTask_Description_Updates_plugin_to_the_latest_version", Plugin.Instance.DefaultUICulture);

        public string Category => Resources.ResourceManager.GetString("PluginOptions_EditorTitle_Strm_Assistant",
            Plugin.Instance.DefaultUICulture);

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Fixed default: daily at 3:00 AM
            // 原版使用 Random.Shared 导致每次调用产生不同值，
            // 热重载后新ID找不到旧配置文件 → 回退到GetDefaultTriggers →
            // 每次生成随机触发器覆盖用户配置
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            };
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await Task.Delay(0).ConfigureAwait(false);
            progress.Report(0);

            try
            {
                var githubToken= Plugin.Instance.MainOptionsStore.GetOptions().AboutOptions.GitHubToken;

                using var response = await _httpClient.SendAsync(new HttpRequestOptions
                {
                    Url = RepoReleaseUrl,
                    CancellationToken = cancellationToken,
                    AcceptHeader = "application/json",
                    UserAgent = Plugin.Instance.UserAgent,
                    EnableDefaultUserAgent = false,
                    RequestHeaders =
                    {
                        ["Authorization"] = !string.IsNullOrWhiteSpace(githubToken)
                            ? $"token {githubToken}"
                            : null
                    }
                }, "GET").ConfigureAwait(false);

                await using var contentStream = response.Content;

                var apiResult = _jsonSerializer.DeserializeFromStream<ApiResponseInfo>(contentStream);

                var currentVersion = ParseVersion(Plugin.Instance.CurrentVersion);
                var remoteVersion = ParseVersion(apiResult?.tag_name);

                if (currentVersion.CompareTo(remoteVersion) < 0)
                {
                    _logger.Info("Found new plugin version: {0}", remoteVersion);

                    var url = (apiResult?.assets ?? new List<ApiAssetInfo>())
                        .FirstOrDefault(asset => asset.name == PluginAssemblyFilename)
                        ?.browser_download_url;
                    if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) throw new Exception("Invalid download url");

                    var githubProxy = Plugin.Instance.MainOptionsStore.GetOptions().AboutOptions.GitHubProxy;

                    await using (var responseStream = await _httpClient.Get(new HttpRequestOptions
                                     {
                                         Url = string.IsNullOrWhiteSpace(githubProxy)
                                             ? url
                                             : $"{githubProxy.TrimEnd('/')}/{url.TrimStart('/')}",
                                         CancellationToken = cancellationToken,
                                         UserAgent = Plugin.Instance.UserAgent,
                                         EnableDefaultUserAgent = false,
                                         Progress = progress,
                                         RequestHeaders =
                                         {
                                             ["Authorization"] = !string.IsNullOrWhiteSpace(githubToken)
                                                 ? $"token {githubToken}"
                                                 : null
                                         }
                                     }).ConfigureAwait(false))
                    {
                        var dllFilePath = Path.Combine(_applicationPaths.PluginsPath, PluginAssemblyFilename);

                        await using (var fileStream =
                                     new FileStream(dllFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await responseStream.CopyToAsync(fileStream, 81920, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    _logger.Info("Plugin update complete");

                    _activityManager.Create(new ActivityLogEntry
                    {
                        Name = string.Format(_localizationManager.GetLocalizedString("XUpdatedOnTo"), Category,
                            remoteVersion, _serverApplicationHost.FriendlyName),
                        Type = "PluginUpdateInstalled",
                        Severity = LogSeverity.Info
                    });

                    _applicationHost.NotifyPendingRestart();
                }
                else
                {
                    _ = Plugin.NotificationApi.SendMessageToAdmins(
                        $"[{Resources.PluginOptions_EditorTitle_Strm_Assistant}] {Resources.No_Update_Message}", 1000);
                    _logger.Info("No need to update");
                }
            }
            catch (Exception e)
            {
                _activityManager.Create(new ActivityLogEntry
                {
                    Name = string.Format(_localizationManager.GetLocalizedString("NameInstallFailedOn"), Category,
                        _serverApplicationHost.FriendlyName),
                    Type = "PluginUpdateFailed",
                    Overview = e.Message,
                    Severity = LogSeverity.Error
                });

                _ = Plugin.NotificationApi.SendMessageToAdmins(
                    $"[{Resources.PluginOptions_EditorTitle_Strm_Assistant}] {Resources.Update_Failed_Message}", 1000);
                _logger.Error("Update failed: {0}", e.Message);
                _logger.Debug(e.StackTrace);
            }

            progress.Report(100);
        }

        private static Version ParseVersion(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return new Version(0, 0);
            try
            {
                return new Version(v.StartsWith("v") ? v.Substring(1) : v);
            }
            catch (ArgumentException)
            {
                return new Version(0, 0);
            }
        }

        public bool IsEnabled => true;
        public bool IsHidden => false;
        public bool IsLogged => true;

        internal class ApiResponseInfo
        {
            public string tag_name { get; set; }

            public List<ApiAssetInfo> assets { get; set; }
        }

        internal class ApiAssetInfo
        {
            public string name { get; set; }

            public string browser_download_url { get; set; }
        }
    }
}
