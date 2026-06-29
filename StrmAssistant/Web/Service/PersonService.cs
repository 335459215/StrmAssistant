using System;
using MediaBrowser.Controller.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using StrmAssistant.Web.Api;

namespace StrmAssistant.Web.Service
{
    public class PersonService : BaseApiService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public PersonService(ILibraryManager libraryManager)
        {
            _logger = Plugin.Instance.Logger;
            _libraryManager = libraryManager;
        }

        public void Any(DeletePerson request)
        {
            try
            {
                var item = _libraryManager.GetItemById(request.Id);

                if (item == null)
                {
                    _logger.Warn("DeletePerson - Item not found: {0}", request.Id);
                    return;
                }

                if (!(item is Person))
                {
                    _logger.Warn("DeletePerson - Item is not a Person: {0} ({1})", item.Name, item.GetType().Name);
                    return;
                }

                var user = GetUserForRequest(null, true);
                if (user == null || !user.Policy.IsAdministrator)
                {
                    throw new ArgumentException("DeletePerson requires administrator privileges");
                }

                var deleteOptions = new DeleteOptions
                {
                    DeleteFromExternalProvider = false,
                    DeleteFileLocation = false
                };

                _libraryManager.DeleteItem(item, deleteOptions, false);
                _logger.Info("DeletePerson - Deleted person: {0}", item.Name);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("DeletePerson error for item {0}", ex, request.Id);
            }
        }
    }
}
