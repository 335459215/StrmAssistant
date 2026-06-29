using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using StrmAssistant.Web.Api;

namespace StrmAssistant.Web.Service
{
    public class ChapterService : BaseApiService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepository;

        public ChapterService(ILibraryManager libraryManager, IItemRepository itemRepository)
        {
            _logger = Plugin.Instance.Logger;
            _libraryManager = libraryManager;
            _itemRepository = itemRepository;
        }

        public void Post(ClearIntro request)
        {
            var itemById = _libraryManager.GetItemById(request.Id);

            if (!(itemById is Series || itemById is Season)) return;

            var episodes = Plugin.ChapterApi.FetchClearTaskItems(new List<BaseItem> { itemById });

            foreach (var item in episodes)
            {
                Plugin.ChapterApi.RemoveIntroCreditsMarkers(item);
                _logger.Info("IntroSkipClear - " + item.Name + " - " + item.Path);
            }
        }

        public void Post(ClearChapterImage request)
        {
            try
            {
                var item = _libraryManager.GetItemById(request.Id);
                if (item == null)
                {
                    _logger.Warn("ClearChapterImage - Item not found: {0}", request.Id);
                    return;
                }

                var videos = GetVideoItems(item);
                var updated = false;

                foreach (var video in videos)
                {
                    var chapters = _itemRepository.GetChapters(video).ToList();
                    var chapterChanged = false;

                    for (int i = 0; i < chapters.Count; i++)
                    {
                        var chapter = chapters[i];
                        if (!string.IsNullOrEmpty(chapter.ImagePath))
                        {
                            chapter.ImagePath = null;
                            chapter.ImageDateModified = default(DateTimeOffset);
                            chapter.ImageTag = null;
                            chapters[i] = chapter;
                            chapterChanged = true;
                        }
                    }

                    if (chapterChanged)
                    {
                        _itemRepository.SaveChapters(video.InternalId, chapters);
                        _libraryManager.UpdateItems(new List<BaseItem> { video }, null,
                            ItemUpdateType.ImageUpdate, true, false, null, CancellationToken.None);
                        updated = true;
                        _logger.Info("ClearChapterImage - Cleared images for: {0}", video.Name);
                    }
                }

                if (updated)
                {
                    _logger.Info("ClearChapterImage - Completed for item: {0}", item.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("ClearChapterImage error for item {0}", ex, request.Id);
            }
        }

        private List<Video> GetVideoItems(BaseItem item)
        {
            if (item is Video video)
                return new List<Video> { video };

            if (item is Series || item is Season)
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    HasExtraType = item.ExtraType.HasValue,
                    PresentationUniqueKey = item.PresentationUniqueKey
                });

                return items.OfType<Video>()
                    .Where(v => v.IsFileProtocol)
                    .ToList();
            }

            return new List<Video>();
        }
    }
}
