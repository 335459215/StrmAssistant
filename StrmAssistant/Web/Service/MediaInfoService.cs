using MediaBrowser.Controller.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using StrmAssistant.Web.Api;
using System;

namespace StrmAssistant.Web.Service
{
    public class MediaInfoService : BaseApiService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public MediaInfoService(ILibraryManager libraryManager)
        {
            _logger = Plugin.Instance.Logger;
            _libraryManager = libraryManager;
        }

        public MediaInfoResponse Get(GetMediaInfo request)
        {
            try
            {
                var item = _libraryManager.GetItemById(request.Id);
                if (item == null)
                {
                    return new MediaInfoResponse
                    {
                        Id = request.Id,
                        HasMediaInfo = false,
                        Error = "Item not found"
                    };
                }

                var mediaSources = Plugin.MediaInfoApi.GetStaticMediaSources(item, false);
                var hasMediaInfo = mediaSources != null && mediaSources.Count > 0 &&
                                   mediaSources[0].MediaStreams?.Count > 0;

                return new MediaInfoResponse
                {
                    Id = item.Id.ToString(),
                    Name = item.Name,
                    Path = item.Path,
                    HasMediaInfo = hasMediaInfo
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorException("MediaInfoService Get error for item {0}", ex, request.Id);
                return new MediaInfoResponse
                {
                    Id = request.Id,
                    HasMediaInfo = false,
                    Error = ex.Message
                };
            }
        }

        public void Post(ExtractMediaInfo request)
        {
            try
            {
                var item = _libraryManager.GetItemById(request.Id);
                if (item == null)
                {
                    _logger.Warn("MediaInfoService - Item not found: {0}", request.Id);
                    return;
                }

                _logger.Info("MediaInfoService - ExtractMediaInfo requested for: {0}", item.Name);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("MediaInfoService Post error for item {0}", ex, request.Id);
            }
        }

        public void Delete(DeleteMediaInfoRequest request)
        {
            try
            {
                var item = _libraryManager.GetItemById(request.Id);
                if (item == null)
                {
                    _logger.Warn("MediaInfoService - Item not found: {0}", request.Id);
                    return;
                }

                _logger.Info("MediaInfoService - DeleteMediaInfo requested for: {0}", item.Name);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("MediaInfoService Delete error for item {0}", ex, request.Id);
            }
        }
    }
}
