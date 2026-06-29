using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace StrmAssistant.Provider
{
    public class BangumiExternalId : IExternalId
    {
        public string Name => "Bangumi";

        public string Key => StaticName;

        public string UrlFormatString => "https://bgm.tv/subject/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series || item is Movie;
        }

        public static string StaticName => "Bangumi";
    }
}
