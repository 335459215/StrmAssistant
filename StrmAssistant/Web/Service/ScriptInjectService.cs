using System;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using StrmAssistant.Web.Api;
using StrmAssistant.Web.Helper;

namespace StrmAssistant.Web.Service
{
    [Unauthenticated]
    public class ScriptInjectService : IService, IRequiresRequest
    {
        private readonly IHttpResultFactory _resultFactory;

        public ScriptInjectService(IHttpResultFactory resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public IRequest Request { get; set; }

        private object ReturnJs(string content)
        {
            return _resultFactory.GetResult(content.AsSpan(), "application/x-javascript");
        }

        // --- MediaInfo.js: Always inject, with douban rating check ---
        public object Get(GetMediaInfoJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedMediaInfoJs(true);
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalMediaInfoJs());
            return ReturnJs(modified);
        }

        // --- Item.js: Always inject (enhanced with StrmAssistant actions) ---
        public object Get(GetItemJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedItemJs();
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalItemJs());
            return ReturnJs(modified);
        }

        // --- LinkedItems.js: Conditional on douban rating ---
        public object Get(GetLinkedItemsJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedLinkedItemsJs(true);
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalLinkedItemsJs());
            return ReturnJs(modified);
        }

        // --- List.js: Always inject ---
        public object Get(GetListJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedListJs();
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalListJs());
            return ReturnJs(modified);
        }

        // --- Indicators.js: Always inject (UI function options depend on config) ---
        public object Get(GetIndicatorJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedIndicatorsJs();
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalIndicatorsJs());
            return ReturnJs(modified);
        }

        // --- RefreshDialog.js: Always inject ---
        public object Get(GetRefreshDialogJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedRefreshDialogJs();
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalRefreshDialogJs());
            return ReturnJs(modified);
        }

        // --- ListController.js: Conditional on douban rating ---
        public object Get(GetListControllerJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedListControllerJs(true);
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalListControllerJs());
            return ReturnJs(modified);
        }

        // --- FilterMenu.js: Conditional on douban assist enabled ---
        public object Get(GetFilterMenuHtml request)
        {
            var modified = ScriptInjectHelper.GetModifiedFilterMenuJs(true);
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalFilterMenuJs());
            return ReturnJs(modified);
        }

        // --- LibraryOptionsEditor.js: Always inject ---
        public object Get(GetLibraryOptionsEditorJs request)
        {
            var modified = ScriptInjectHelper.GetModifiedLibraryOptionsEditorJs();
            if (modified == null) return ReturnJs(ScriptInjectHelper.GetOriginalLibraryOptionsEditorJs());
            return ReturnJs(modified);
        }
    }
}
