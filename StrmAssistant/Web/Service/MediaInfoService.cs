using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using StrmAssistant.Web.Api;

namespace StrmAssistant.Web.Service
{
    public class MediaInfoService : BaseApiService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepository;

        public MediaInfoService(ILibraryManager libraryManager, IItemRepository itemRepository)
        {
            _logger = Plugin.Instance.Logger;
            _libraryManager = libraryManager;
            _itemRepository = itemRepository;
        }

        public void Post(ClearMediaInfo request)
        {
            try
            {
                var item = _libraryManager.GetItemById(request.Id);
                if (item == null)
                {
                    _logger.Warn("ClearMediaInfo - Item not found: {0}", request.Id);
                    return;
                }

                var items = GetMediaItems(item);
                var updated = false;

                foreach (var baseItem in items)
                {
                    if (!(baseItem is Video video)) continue;

                    var mediaSources = Plugin.MediaInfoApi.GetStaticMediaSources(video, false);
                    if (mediaSources == null || mediaSources.Count == 0) continue;

                    foreach (var mediaSource in mediaSources)
                    {
                        if (mediaSource.MediaStreams != null && mediaSource.MediaStreams.Count > 0)
                        {
                            mediaSource.MediaStreams.Clear();
                        }
                    }

                    _libraryManager.UpdateItems(new List<BaseItem> { video }, null,
                        ItemUpdateType.MetadataImport, true, false, null, CancellationToken.None);
                    updated = true;
                    _logger.Info("ClearMediaInfo - Cleared info for: {0}", video.Name);
                }

                if (updated)
                {
                    _logger.Info("ClearMediaInfo - Completed for item: {0}", item.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("ClearMediaInfo error for item {0}", ex, request.Id);
            }
        }

        public object Post(SyncMediaInfo request)
        {
            try
            {
                BaseItem item = null;

                if (!string.IsNullOrEmpty(request.Id))
                {
                    item = _libraryManager.GetItemById(request.Id);
                }

                if (item == null && !string.IsNullOrEmpty(request.Path))
                {
                    item = _libraryManager.FindByPath(request.Path, false);
                }

                if (item == null)
                {
                    return new List<MediaInfoBundle>();
                }

                if (item is Episode episode)
                {
                    var mediaSources = Plugin.MediaInfoApi.GetStaticMediaSources(episode, false);
                    var result = new List<MediaInfoBundle>();

                    if (mediaSources != null)
                    {
                        foreach (var mediaSource in mediaSources)
                        {
                            var chapters = _itemRepository.GetChapters(episode).ToList();

                            var bundle = new MediaInfoBundle
                            {
                                MediaSourceInfo = mediaSource,
                                Chapters = chapters,
                                ZeroFingerprintConfidence = null,
                                EmbeddedImage = null
                            };

                            result.Add(bundle);
                        }
                    }

                    _logger.Info("SyncMediaInfo - Synced {0} sources for: {1}", result.Count, episode.Name);
                    return result;
                }

                return new List<MediaInfoBundle>();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("SyncMediaInfo error for item {0}", ex, request.Id ?? request.Path);
                return new List<MediaInfoBundle>();
            }
        }

        private List<BaseItem> GetMediaItems(BaseItem item)
        {
            if (item is Video)
                return new List<BaseItem> { item };

            if (item is Series || item is Season)
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    HasExtraType = item.ExtraType.HasValue,
                    PresentationUniqueKey = item.PresentationUniqueKey
                });

                return items.Where(v => v is Video && v.IsFileProtocol).ToList();
            }

            return new List<BaseItem>();
        }
    }
}
