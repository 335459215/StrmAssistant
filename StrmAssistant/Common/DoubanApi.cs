using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using StrmAssistant.Dto.Douban;
using StrmAssistant.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace StrmAssistant.Common
{
    public class DoubanApi
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;

        private static readonly LruCache LruCache = new LruCache(200);
        private static long _lastRequestTicks;
        private const int RequestIntervalMs = 200;

        public static readonly TimeSpan DefaultCacheTime = TimeSpan.FromHours(6.0);

        private const string DetailApiUrl = "https://frodo.douban.com/api/v2/movie/{0}";
        private const string SeasonsApiUrl = "https://frodo.douban.com/api/v2/tv/{0}/seasons";
        private const string CelebrityApiUrl = "https://frodo.douban.com/api/v2/movie/{0}/celebrities";
        private const string SearchApiUrl = "https://frodo.douban.com/api/v2/search?q={0}&count=5";

        private static readonly Dictionary<string, string> DefaultHeaders = new Dictionary<string, string>
        {
            { "User-Agent", "MicroMessenger/" },
            { "Referer", "https://servicewechat.com/wx2f9b06c18de2548b/91/page-frame.html" }
        };

        public DoubanApi(IJsonSerializer jsonSerializer, IHttpClient httpClient,
            ILibraryManager libraryManager)
        {
            _logger = Plugin.Instance.Logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
        }

        public async Task<DoubanDetailResponse> GetDoubanDetail(string doubanId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(doubanId)) return null;

            var cacheKey = $"detail_{doubanId}";
            if (LruCache.TryGetFromCache(cacheKey, out DoubanDetailResponse cached))
            {
                return cached;
            }

            try
            {
                await ThrottleRequest(cancellationToken).ConfigureAwait(false);

                var url = string.Format(DetailApiUrl, doubanId);
                var response = await FetchJsonAsync<DoubanDetailResponse>(url, cancellationToken).ConfigureAwait(false);

                if (response != null)
                {
                    LruCache.AddOrUpdateCache(cacheKey, response);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.Debug($"DoubanApi - GetDoubanDetail failed for {doubanId}: {ex.Message}");
                return null;
            }
        }

        public async Task<DoubanSeasonsResponse> GetDoubanSeasons(string doubanId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(doubanId)) return null;

            var cacheKey = $"seasons_{doubanId}";
            if (LruCache.TryGetFromCache(cacheKey, out DoubanSeasonsResponse cached))
            {
                return cached;
            }

            try
            {
                await ThrottleRequest(cancellationToken).ConfigureAwait(false);

                var url = string.Format(SeasonsApiUrl, doubanId);
                var response = await FetchJsonAsync<DoubanSeasonsResponse>(url, cancellationToken).ConfigureAwait(false);

                if (response != null)
                {
                    LruCache.AddOrUpdateCache(cacheKey, response);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.Debug($"DoubanApi - GetDoubanSeasons failed for {doubanId}: {ex.Message}");
                return null;
            }
        }

        public async Task<DoubanCelebrityResponse> GetDoubanCelebrities(string doubanId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(doubanId)) return null;

            var cacheKey = $"celebrities_{doubanId}";
            if (LruCache.TryGetFromCache(cacheKey, out DoubanCelebrityResponse cached))
            {
                return cached;
            }

            try
            {
                await ThrottleRequest(cancellationToken).ConfigureAwait(false);

                var url = string.Format(CelebrityApiUrl, doubanId);
                var response = await FetchJsonAsync<DoubanCelebrityResponse>(url, cancellationToken).ConfigureAwait(false);

                if (response != null)
                {
                    LruCache.AddOrUpdateCache(cacheKey, response);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.Debug($"DoubanApi - GetDoubanCelebrities failed for {doubanId}: {ex.Message}");
                return null;
            }
        }

        public async Task<DoubanAbstractResponse> SearchDouban(string query,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            try
            {
                await ThrottleRequest(cancellationToken).ConfigureAwait(false);

                var url = string.Format(SearchApiUrl, Uri.EscapeDataString(query));
                return await FetchJsonAsync<DoubanAbstractResponse>(url, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Debug($"DoubanApi - SearchDouban failed for '{query}': {ex.Message}");
                return null;
            }
        }

        public async Task<string> FindDoubanId(BaseItem item, CancellationToken cancellationToken)
        {
            var existingId = item.GetProviderId(DoubanExternalId.StaticName);
            if (!string.IsNullOrWhiteSpace(existingId)) return existingId;

            var searchQuery = BuildSearchQuery(item);
            if (string.IsNullOrWhiteSpace(searchQuery)) return null;

            var searchResult = await SearchDouban(searchQuery, cancellationToken).ConfigureAwait(false);
            if (searchResult?.Subjects == null || searchResult.Subjects.Count == 0) return null;

            var year = item.ProductionYear;
            var isTv = item is Series || item is Season;

            foreach (var subject in searchResult.Subjects)
            {
                if (subject.Id == null) continue;

                if (isTv && !subject.IsTv) continue;
                if (!isTv && subject.IsTv) continue;

                if (year.HasValue && !string.IsNullOrEmpty(subject.ReleaseYear)
                    && int.TryParse(subject.ReleaseYear, out var resultYear)
                    && Math.Abs(resultYear - year.Value) > 1) continue;

                return subject.Id;
            }

            return searchResult.Subjects.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Id))?.Id;
        }

        private static string BuildSearchQuery(BaseItem item)
        {
            var name = item.OriginalTitle ?? item.Name ?? item.SortName;
            if (string.IsNullOrWhiteSpace(name)) return null;

            var year = item.ProductionYear;
            return year.HasValue ? $"{name} {year.Value}" : name;
        }

        public async Task<(DoubanDetailResponse detail, string doubanId)> GetDoubanDetailForItem(
            BaseItem item, CancellationToken cancellationToken)
        {
            var doubanId = await FindDoubanId(item, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(doubanId)) return (null, null);

            var detail = await GetDoubanDetail(doubanId, cancellationToken).ConfigureAwait(false);
            return (detail, doubanId);
        }

        private async Task<T> FetchJsonAsync<T>(string url, CancellationToken cancellationToken) where T : class
        {
            try
            {
                var options = new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken,
                    AcceptHeader = "application/json",
                    BufferContent = true,
                    UserAgent = DefaultHeaders["User-Agent"]
                };

                options.RequestHeaders["Referer"] = DefaultHeaders["Referer"];

                using (var response = await _httpClient.SendAsync(options, "GET").ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK) return null;

                    await using var contentStream = response.Content;
                    var result = _jsonSerializer.DeserializeFromStream<T>(contentStream);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"DoubanApi - FetchJsonAsync<{typeof(T).Name}> failed: {ex.Message}");
                return null;
            }
        }

        private static async Task ThrottleRequest(CancellationToken cancellationToken)
        {
            var ticks = Interlocked.Read(ref _lastRequestTicks);
            var elapsedMs = (DateTimeOffset.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond;
            var delay = RequestIntervalMs - (int)elapsedMs;

            if (delay > 0)
            {
                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }

            Interlocked.Exchange(ref _lastRequestTicks, DateTimeOffset.UtcNow.Ticks);
        }

        public List<BaseItem> FetchDoubanCacheItems()
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
                IsVirtualItem = false,
                Recursive = true
            };

            var items = _libraryManager.GetItemList(query);
            return items.Where(i => !string.IsNullOrWhiteSpace(i.GetProviderId(DoubanExternalId.StaticName))
                                    || !string.IsNullOrWhiteSpace(i.Name))
                .ToList();
        }

        public void UpdateItem(BaseItem item)
        {
            _libraryManager.UpdateItems(new List<BaseItem> { item }, null,
                ItemUpdateType.None, true, false, null, CancellationToken.None);
        }
    }
}
