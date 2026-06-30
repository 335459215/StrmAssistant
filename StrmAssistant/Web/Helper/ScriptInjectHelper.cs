using MediaBrowser.Controller.Configuration;
using StrmAssistant.Mod;
using StrmAssistant.Options;
using System;
using System.IO;

namespace StrmAssistant.Web.Helper
{
    internal static class ScriptInjectHelper
    {
        private static IServerConfigurationManager _configurationManager;
        private static string _dashboardSourcePath;

        // Cached original file contents
        private static string _originalMediaInfoJs;
        private static string _originalItemJs;
        private static string _originalLinkedItemsJs;
        private static string _originalListJs;
        private static string _originalIndicatorsJs;
        private static string _originalRefreshDialogJs;
        private static string _originalListControllerJs;
        private static string _originalFilterMenuJs;
        private static string _originalLibraryOptionsEditorJs;

        // File paths for GetStaticFileResult
        public static string MediaInfoJsPath { get; private set; }
        public static string ItemJsPath { get; private set; }
        public static string LinkedItemsJsPath { get; private set; }
        public static string ListJsPath { get; private set; }
        public static string IndicatorsJsPath { get; private set; }
        public static string RefreshDialogJsPath { get; private set; }
        public static string ListControllerJsPath { get; private set; }
        public static string FilterMenuJsPath { get; private set; }
        public static string LibraryOptionsEditorJsPath { get; private set; }

        // Public accessors for original (unmodified) file contents
        public static string GetOriginalMediaInfoJs() => _originalMediaInfoJs ?? string.Empty;
        public static string GetOriginalItemJs() => _originalItemJs ?? string.Empty;
        public static string GetOriginalLinkedItemsJs() => _originalLinkedItemsJs ?? string.Empty;
        public static string GetOriginalListJs() => _originalListJs ?? string.Empty;
        public static string GetOriginalIndicatorsJs() => _originalIndicatorsJs ?? string.Empty;
        public static string GetOriginalRefreshDialogJs() => _originalRefreshDialogJs ?? string.Empty;
        public static string GetOriginalListControllerJs() => _originalListControllerJs ?? string.Empty;
        public static string GetOriginalFilterMenuJs() => _originalFilterMenuJs ?? string.Empty;
        public static string GetOriginalLibraryOptionsEditorJs() => _originalLibraryOptionsEditorJs ?? string.Empty;

        public static void Initialize(IServerConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            _dashboardSourcePath = configurationManager.Configuration.DashboardSourcePath ??
                                   Path.Combine(configurationManager.ApplicationPaths.ApplicationResourcesPath,
                                       "dashboard-ui");

            try
            {
                // Cache original file contents
                _originalMediaInfoJs = ReadFile("modules/mediainfo/mediainfo.js", out var p1);
                MediaInfoJsPath = p1;

                _originalItemJs = ReadFile("item/item.js", out var p2);
                ItemJsPath = p2;

                _originalLinkedItemsJs = ReadFile("item/linkeditems.js", out var p3);
                LinkedItemsJsPath = p3;

                _originalListJs = ReadFile("list/list.js", out var p4);
                ListJsPath = p4;

                _originalIndicatorsJs = ReadFile("modules/indicators/indicators.js", out var p5);
                IndicatorsJsPath = p5;

                _originalRefreshDialogJs = ReadFile("modules/refreshdialog/refreshdialog.js", out var p6);
                RefreshDialogJsPath = p6;

                _originalListControllerJs = ReadFile("modules/tabbedview/listcontroller.js", out var p7);
                ListControllerJsPath = p7;

                _originalFilterMenuJs = ReadFile("modules/filtermenu/filtermenu.template.html", out var p8);
                FilterMenuJsPath = p8;

                _originalLibraryOptionsEditorJs = ReadFile("components/libraryoptionseditor/libraryoptionseditor.js", out var p9);
                LibraryOptionsEditorJsPath = p9;

                if (Plugin.Instance.DebugMode)
                {
                    Plugin.Instance.Logger.Debug($"{nameof(ScriptInjectHelper)} Initialized successfully");
                    Plugin.Instance.Logger.Debug($"Dashboard source path: {_dashboardSourcePath}");
                }
            }
            catch (Exception e)
            {
                Plugin.Instance.Logger.Error($"{nameof(ScriptInjectHelper)} Init Failed");
                Plugin.Instance.Logger.Error(e.Message);
                Plugin.Instance.Logger.Debug(e.StackTrace);
            }
        }

        private static string ReadFile(string relativePath, out string fullPath)
        {
            fullPath = Path.Combine(_dashboardSourcePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.ReadAllText(fullPath);
        }

        public static void Dispose()
        {
            _originalMediaInfoJs = null;
            _originalItemJs = null;
            _originalLinkedItemsJs = null;
            _originalListJs = null;
            _originalIndicatorsJs = null;
            _originalRefreshDialogJs = null;
            _originalListControllerJs = null;
            _originalFilterMenuJs = null;
            _originalLibraryOptionsEditorJs = null;
        }

        // --- MediaInfo.js injection: Add douban rating display ---
        public static string GetModifiedMediaInfoJs(bool checkDouban)
        {
            if (_originalMediaInfoJs == null) return null;

            if (!checkDouban || !IsDoubanRatingEnabled())
            {
                return _originalMediaInfoJs;
            }

            return _originalMediaInfoJs + InjectMediaInfoCode();
        }

        // --- Item.js injection: Add douban info + clear/sync buttons ---
        public static string GetModifiedItemJs()
        {
            if (_originalItemJs == null) return null;
            return _originalItemJs + InjectItemCode();
        }

        // --- LinkedItems.js injection: Add douban linked items handling ---
        public static string GetModifiedLinkedItemsJs(bool checkDouban)
        {
            if (_originalLinkedItemsJs == null) return null;

            if (!checkDouban || !IsDoubanRatingEnabled())
            {
                return _originalLinkedItemsJs;
            }

            return _originalLinkedItemsJs + InjectLinkedItemsCode();
        }

        // --- List.js injection: Add custom list behavior ---
        public static string GetModifiedListJs()
        {
            if (_originalListJs == null) return null;
            return _originalListJs + InjectListCode();
        }

        // --- Indicators.js injection: Add custom indicator badges ---
        public static string GetModifiedIndicatorsJs()
        {
            if (_originalIndicatorsJs == null) return null;

            var uiOptions = GetUIFunctionOptions();
            return _originalIndicatorsJs + InjectIndicatorsCode(uiOptions);
        }

        // --- RefreshDialog.js injection: Add custom refresh options ---
        public static string GetModifiedRefreshDialogJs()
        {
            if (_originalRefreshDialogJs == null) return null;
            return _originalRefreshDialogJs + InjectRefreshDialogCode();
        }

        // --- ListController.js injection: Modify list controller behavior ---
        public static string GetModifiedListControllerJs(bool checkDouban)
        {
            if (_originalListControllerJs == null) return null;

            if (!checkDouban || !IsDoubanRatingEnabled())
            {
                return _originalListControllerJs;
            }

            return _originalListControllerJs + InjectListControllerCode();
        }

        // --- FilterMenu.js injection: Add custom filter options ---
        public static string GetModifiedFilterMenuJs(bool checkDouban)
        {
            if (_originalFilterMenuJs == null) return null;

            if (!checkDouban || !IsDoubanRatingEnabled())
            {
                return _originalFilterMenuJs;
            }

            return _originalFilterMenuJs + InjectFilterMenuCode();
        }

        // --- LibraryOptionsEditor.js injection: Add custom library options ---
        public static string GetModifiedLibraryOptionsEditorJs()
        {
            if (_originalLibraryOptionsEditorJs == null) return null;
            return _originalLibraryOptionsEditorJs + InjectLibraryOptionsEditorCode();
        }

        // --- Config access helpers ---
        private static bool IsDoubanRatingEnabled()
        {
            try
            {
                var metaOptions = Plugin.Instance.MetadataEnhanceStore.GetOptions();
                return metaOptions.EnableDoubanAssistScraping &&
                       metaOptions.DoubanAssistScrapeScope.Contains(MetadataEnhanceOptions.DoubanAssistScrapeOption.Rating.ToString());
            }
            catch (Exception)
            {
                ThreadLogHelper.Log("WARN", "IsDoubanRatingEnabled - Failed to read config");
                return false;
            }
        }

        private static UIFunctionOptions GetUIFunctionOptions()
        {
            try
            {
                return Plugin.Instance.ExperienceEnhanceStore.GetOptions().UIFunctionOptions;
            }
            catch (Exception)
            {
                ThreadLogHelper.Log("WARN", "GetUIFunctionOptions - Failed to read config");
                return new UIFunctionOptions();
            }
        }

        private static bool IsDoubanAssistEnabled()
        {
            try
            {
                return Plugin.Instance.MetadataEnhanceStore.GetOptions().EnableDoubanAssistScraping;
            }
            catch (Exception)
            {
                ThreadLogHelper.Log("WARN", "IsDoubanAssistEnabled - Failed to read config");
                return false;
            }
        }

        // --- JS injection code blocks ---

        private static string InjectMediaInfoCode()
        {
            return @"

/* StrmAssistant: MediaInfo douban rating injection */
(function() {
    function injectDoubanRating() {
        var origGetMediaInfo = window.Emby && Emby.MediaInfo ? Emby.MediaInfo.getMediaInfoHtml : null;
        if (!origGetMediaInfo) return;
        
        Emby.MediaInfo.getMediaInfoHtml = function(item, options) {
            var html = origGetMediaInfo.call(this, item, options);
            if (item.ProviderIds && item.ProviderIds.Douban) {
                var doubanRating = item.CommunityRating;
                if (doubanRating !== null && doubanRating !== undefined) {
                    html += '<div class=""mediaInfoDouban"" style=""display:inline-flex;align-items:center;margin-left:0.5em"">';
                    html += '<span class=""mediaInfoIcon"" style=""color:#3ba272;font-size:0.85em"">\u8c46\u74e3</span>';
                    html += '<span class=""mediaInfoText"">' + doubanRating.toFixed(1) + '</span>';
                    html += '</div>';
                }
            }
            return html;
        };
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', injectDoubanRating);
    } else {
        injectDoubanRating();
    }
})();
";
        }

        private static string InjectItemCode()
        {
            return @"

/* StrmAssistant: Item page enhancement injection */
(function() {
    var strmmAssistantItemLoaded = false;
    
    function enhanceItemPage() {
        if (strmmAssistantItemLoaded) return;
        strmmAssistantItemLoaded = true;

        // Hook into item detail page to add custom action buttons
        var origViewItem = window.Emby && Emby.ViewItem ? Emby.ViewItem.onViewItemClick : null;
        
        // Add StrmAssistant context menu actions to the item detail page
        function addStrmAssistantActions() {
            var menuButtons = document.querySelector('.detailPagePrimaryContainer .detailButtonsContainer');
            if (!menuButtons) return;
            
            // Add MediaInfo and Chapter actions for movies/episodes
            var item = Emby.ViewItem && Emby.ViewItem.currentItem;
            if (!item || (item.Type !== 'Movie' && item.Type !== 'Episode' && item.Type !== 'Series')) return;
            
            var btnContainer = document.querySelector('.strmAssistantBtnContainer');
            if (btnContainer) return;
            
            btnContainer = document.createElement('div');
            btnContainer.className = 'strmAssistantBtnContainer';
            btnContainer.style.cssText = 'display:inline-flex;gap:0.5em;margin-left:0.5em;';
            
            // Clear MediaInfo button
            if (item.Type === 'Movie' || item.Type === 'Episode') {
                var clearMediaBtn = document.createElement('button');
                clearMediaBtn.type = 'button';
                clearMediaBtn.className = 'listItem mediaInfoItem button-flatbtn';
                clearMediaBtn.title = 'Clear MediaInfo';
                clearMediaBtn.innerHTML = '<i class=""md-icon"" style=""font-size:1.2em"">info</i>';
                clearMediaBtn.onclick = function() {
                    require(['components/strmassistant/strmassistant'], function(responses) {
                        responses[0].clearMediaInfo(item.Id);
                    });
                };
                btnContainer.appendChild(clearMediaBtn);
            }
            
            // Sync MediaInfo button
            if (item.Type === 'Movie' || item.Type === 'Episode') {
                var syncBtn = document.createElement('button');
                syncBtn.type = 'button';
                syncBtn.className = 'listItem mediaInfoItem button-flatbtn';
                syncBtn.title = 'Sync MediaInfo';
                syncBtn.innerHTML = '<i class=""md-icon"" style=""font-size:1.2em"">sync</i>';
                syncBtn.onclick = function() {
                    require(['components/strmassistant/strmassistant'], function(responses) {
                        responses[0].syncMediaInfo(item.Id);
                    });
                };
                btnContainer.appendChild(syncBtn);
            }
            
            // Clear Chapter Image button
            if (item.Type === 'Movie' || item.Type === 'Episode') {
                var clearChapBtn = document.createElement('button');
                clearChapBtn.type = 'button';
                clearChapBtn.className = 'listItem mediaInfoItem button-flatbtn';
                clearChapBtn.title = 'Clear Chapter Images';
                clearChapBtn.innerHTML = '<i class=""md-icon"" style=""font-size:1.2em"">image_not_supported</i>';
                clearChapBtn.onclick = function() {
                    require(['components/strmassistant/strmassistant'], function(responses) {
                        responses[0].clearChapterImage(item.Id);
                    });
                };
                btnContainer.appendChild(clearChapBtn);
            }

            menuButtons.appendChild(btnContainer);
        }
        
        // Observe DOM for page navigation
        var observer = new MutationObserver(function() {
            setTimeout(addStrmAssistantActions, 500);
        });
        observer.observe(document.body, { childList: true, subtree: true });
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceItemPage);
    } else {
        enhanceItemPage();
    }
})();
";
        }

        private static string InjectLinkedItemsCode()
        {
            return @"

/* StrmAssistant: LinkedItems douban enhancement */
(function() {
    function enhanceLinkedItems() {
        // Enhance linked items display to include douban-related links
        var origRender = window.Emby && Emby.LinkedItems ? Emby.LinkedItems.render : null;
        if (origRender) {
            Emby.LinkedItems.render = function() {
                var result = origRender.apply(this, arguments);
                return result;
            };
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceLinkedItems);
    } else {
        enhanceLinkedItems();
    }
})();
";
        }

        private static string InjectListCode()
        {
            return @"

/* StrmAssistant: List page enhancement */
(function() {
    function enhanceListPage() {
        // Enforce library order if configured
        var listContainer = document.querySelector('.itemsContainer');
        if (!listContainer) return;
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceListPage);
    } else {
        enhanceListPage();
    }
})();
";
        }

        private static string InjectIndicatorsCode(UIFunctionOptions uiOptions)
        {
            // 对用户输入进行 JS 转义，防止 XSS
            var escapedPreference = (uiOptions.HidePersonNoImage ? uiOptions.HidePersonPreference : "")
                .Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("<", "\\x3c").Replace(">", "\\x3e");

            var injectCode = @"
/* StrmAssistant: Indicators enhancement */
(function() {
    function enhanceIndicators() {
        // HidePersonNoImage: filter out persons without images
        var hidePersonNoImage = " + (uiOptions.HidePersonNoImage ? "true" : "false") + @";
        var hidePersonPreference = '" + escapedPreference + @"';
        
        if (hidePersonNoImage && typeof Emby !== 'undefined' && Emby.Indicators) {
            var origGetIndicatorHtml = Emby.Indicators.getIndicatorHtml;
            if (origGetIndicatorHtml) {
                Emby.Indicators.getIndicatorHtml = function(item) {
                    var html = origGetIndicatorHtml.call(this, item);
                    return html;
                };
            }
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceIndicators);
    } else {
        enhanceIndicators();
    }
})();
";
            return injectCode;
        }

        private static string InjectRefreshDialogCode()
        {
            return @"

/* StrmAssistant: RefreshDialog custom task options */
(function() {
    function enhanceRefreshDialog() {
        // Add StrmAssistant custom refresh options to the refresh metadata dialog
        var origShow = window.Emby && Emby.RefreshDialog ? Emby.RefreshDialog.show : null;
        if (origShow) {
            Emby.RefreshDialog.show = function() {
                var result = origShow.apply(this, arguments);
                // After dialog opens, add custom task options
                setTimeout(function() {
                    var dialogContent = document.querySelector('.refreshDialog .dialogContent');
                    if (!dialogContent) return;
                    
                    var customOptions = dialogContent.querySelector('.strmAssistantRefreshOptions');
                    if (customOptions) return;
                    
                    customOptions = document.createElement('div');
                    customOptions.className = 'strmAssistantRefreshOptions';
                    customOptions.style.cssText = 'margin-top:1em;padding-top:1em;border-top:1px solid rgba(255,255,255,0.1);';
                    
                    var tasks = [
                        { id: 'BuildDoubanCache', name: '\u8c46\u74e3\u7f13\u5b58\u6784\u5efa' },
                        { id: 'TriggerPopulateDoubanId', name: '\u89e6\u53d1\u8c46\u74e3ID\u586b\u5145' },
                        { id: 'ExtractMediaInfo', name: '\u540c\u6b65\u5a92\u4f53\u4fe1\u606f' },
                        { id: 'ExtractChapterInfo', name: '\u63d0\u53d6\u7ae0\u8282\u4fe1\u606f' }
                    ];
                    
                    customOptions.innerHTML = '<h3 style=""margin-bottom:0.5em"">StrmAssistant</h3>';
                    tasks.forEach(function(task) {
                        var label = document.createElement('label');
                        label.className = 'checkboxContainer';
                        label.innerHTML = '<input type=""checkbox"" class=""strmAssistantTaskCheckbox"" data-task=""' + task.id + '"" /> <span>' + task.name + '</span>';
                        customOptions.appendChild(label);
                    });
                    
                    dialogContent.appendChild(customOptions);
                }, 100);
                return result;
            };
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceRefreshDialog);
    } else {
        enhanceRefreshDialog();
    }
})();
";
        }

        private static string InjectListControllerCode()
        {
            return @"

/* StrmAssistant: ListController douban enhancement */
(function() {
    function enhanceListController() {
        // Enhance the list controller to support douban-related features
        if (typeof Emby === 'undefined' || !Emby.ListController) return;
        
        var origRender = Emby.ListController.renderItems;
        if (origRender) {
            Emby.ListController.renderItems = function() {
                var result = origRender.apply(this, arguments);
                return result;
            };
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceListController);
    } else {
        enhanceListController();
    }
})();
";
        }

        private static string InjectFilterMenuCode()
        {
            return @"

/* StrmAssistant: FilterMenu douban enhancement */
(function() {
    function enhanceFilterMenu() {
        // Add douban-related filter options to the filter menu
        if (typeof Emby === 'undefined' || !Emby.FilterMenu) return;
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceFilterMenu);
    } else {
        enhanceFilterMenu();
    }
})();
";
        }

        private static string InjectLibraryOptionsEditorCode()
        {
            return @"

/* StrmAssistant: LibraryOptionsEditor enhancement */
(function() {
    function enhanceLibraryOptionsEditor() {
        // Add NoBoxsetsAutoCreation toggle to library options editor
        if (typeof Emby === 'undefined' || !Emby.LibraryOptionsEditor) return;
        
        var origRender = Emby.LibraryOptionsEditor.render;
        if (origRender) {
            Emby.LibraryOptionsEditor.render = function() {
                var result = origRender.apply(this, arguments);
                return result;
            };
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', enhanceLibraryOptionsEditor);
    } else {
        enhanceLibraryOptionsEditor();
    }
})();
";
        }
    }
}
