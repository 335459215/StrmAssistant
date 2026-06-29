using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace StrmAssistant.Provider
{
    public class DoubanExternalId : IExternalId
    {
        public string Name => "Douban";

        public string Key => StaticName;

        public string UrlFormatString => "https://movie.douban.com/subject/{0}/";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series || item is Movie;
        }

        public static string StaticName => "Douban";
    }
}
