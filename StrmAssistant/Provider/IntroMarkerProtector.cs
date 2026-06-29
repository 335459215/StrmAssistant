using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Linq;

namespace StrmAssistant.Provider
{
    /// <summary>
    /// Protects intro/credits marker chapters from being accidentally removed during metadata refresh.
    /// </summary>
    public class IntroMarkerProtector
    {
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepository;

        public IntroMarkerProtector(IItemRepository itemRepository)
        {
            _logger = Plugin.Instance.Logger;
            _itemRepository = itemRepository;
        }

        public bool HasProtectedMarkers(BaseItem item)
        {
            if (!(item is Episode)) return false;
            var chapters = _itemRepository.GetChapters(item);
            return chapters.Any(c =>
                c.MarkerType == MarkerType.IntroStart ||
                c.MarkerType == MarkerType.IntroEnd ||
                c.MarkerType == MarkerType.CreditsStart);
        }

        public List<ChapterInfo> GetProtectedMarkers(BaseItem item)
        {
            var chapters = _itemRepository.GetChapters(item);
            return chapters.Where(c =>
                c.MarkerType == MarkerType.IntroStart ||
                c.MarkerType == MarkerType.IntroEnd ||
                c.MarkerType == MarkerType.CreditsStart).ToList();
        }

        public void RestoreProtectedMarkers(BaseItem item, List<ChapterInfo> protectedMarkers)
        {
            if (protectedMarkers == null || protectedMarkers.Count == 0) return;

            var currentChapters = _itemRepository.GetChapters(item).ToList();
            var restored = false;

            foreach (var marker in protectedMarkers)
            {
                if (!currentChapters.Any(c => c.MarkerType == marker.MarkerType))
                {
                    currentChapters.Add(marker);
                    restored = true;
                    _logger.Info("IntroMarkerProtector - Restored {0} marker for: {1}",
                        marker.MarkerType, item.Name);
                }
            }

            if (restored)
            {
                _itemRepository.SaveChapters(item.InternalId, currentChapters);
            }
        }
    }
}
