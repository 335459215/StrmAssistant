using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using StrmAssistant.Dto.IntroDb;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StrmAssistant.Provider
{
    /// <summary>
    /// Unified intro sequence database provider - queries and submits intro timestamps
    /// to a community-maintained database.
    /// </summary>
    public class UnifiedIntroDbProvider
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HttpClient _httpClient;

        private const string DefaultApiBase = "https://introskipper.anotherstranger.me/v1";

        public UnifiedIntroDbProvider(IJsonSerializer jsonSerializer)
        {
            _logger = Plugin.Instance.Logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        public string ApiBase { get; set; } = DefaultApiBase;
        public string ApiKey { get; set; } = string.Empty;
        public bool AutoSubmit { get; set; }
        public bool AutoUpdate { get; set; }

        public async Task<IntroDbQueryResult> QueryIntroAsync(string seriesName, int seasonNumber, int episodeNumber,
            CancellationToken cancellationToken)
        {
            try
            {
                var url = $"{ApiBase}/episode/intro?series={Uri.EscapeDataString(seriesName)}" +
                          $"&season={seasonNumber}&episode={episodeNumber}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(ApiKey))
                    request.Headers.Add("X-API-Key", ApiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Debug("IntroDb query returned {0} for {1} S{2}E{3}",
                        response.StatusCode, seriesName, seasonNumber, episodeNumber);
                    return new IntroDbQueryResult { Found = false };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var entry = _jsonSerializer.DeserializeFromString<IntroDbEntry>(content);
                return new IntroDbQueryResult { Found = true, Entry = entry };
            }
            catch (Exception e)
            {
                _logger.Debug("IntroDb query failed: {0}", e.Message);
                return new IntroDbQueryResult { Found = false, ErrorMessage = e.Message };
            }
        }

        public async Task<IntroDbSubmitResponse> SubmitIntroAsync(IntroDbSubmitRequest submitRequest,
            CancellationToken cancellationToken)
        {
            try
            {
                var url = $"{ApiBase}/episode/submit";
                var json = _jsonSerializer.SerializeToString(submitRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                if (!string.IsNullOrWhiteSpace(ApiKey))
                    request.Headers.Add("X-API-Key", ApiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return _jsonSerializer.DeserializeFromString<IntroDbSubmitResponse>(responseBody)
                           ?? new IntroDbSubmitResponse { Success = true, Message = "Submitted" };
                }

                _logger.Debug("IntroDb submit returned {0}", response.StatusCode);
                return new IntroDbSubmitResponse { Success = false, Message = $"HTTP {response.StatusCode}" };
            }
            catch (Exception e)
            {
                _logger.Debug("IntroDb submit failed: {0}", e.Message);
                return new IntroDbSubmitResponse { Success = false, Message = e.Message };
            }
        }

        public async Task UpdateIntroFromDbAsync(Episode episode, CancellationToken cancellationToken)
        {
            var seriesName = episode.Series?.Name ?? episode.SeriesName;
            if (string.IsNullOrWhiteSpace(seriesName)) return;

            var result = await QueryIntroAsync(seriesName, episode.ParentIndexNumber ?? 0,
                episode.IndexNumber ?? 0, cancellationToken).ConfigureAwait(false);

            if (result.Found && result.Entry != null && result.Entry.IsValid)
            {
                Plugin.ChapterApi.UpdateIntro(episode, null,
                    result.Entry.IntroStartTicks, result.Entry.IntroEndTicks);
                _logger.Info("IntroDb - Updated intro for {0} from database", episode.Name);
            }
        }
    }
}
