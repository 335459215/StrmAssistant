using HarmonyLib;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using StrmAssistant.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static StrmAssistant.Mod.PatchManager;

namespace StrmAssistant.Mod
{
    public class EnableDeepDelete : PatchBase<EnableDeepDelete>
    {
        private static MethodInfo _deleteItem;

        public EnableDeepDelete()
        {
            Initialize();

            if (Plugin.Instance.ExperienceEnhanceStore.GetOptions().EnableDeepDelete)
            {
                Patch();
            }
        }

        protected override void OnInitialize()
        {
            var embyServerImplementationsAssembly = EmbyVersionAdapter.Instance.TryLoadAssembly("Emby.Server.Implementations");
            if (embyServerImplementationsAssembly == null)
            {
                Plugin.Instance.Logger.Error("EnableDeepDelete: Failed to load Emby.Server.Implementations");
                return;
            }

            var libraryManager =
                embyServerImplementationsAssembly.GetType("Emby.Server.Implementations.Library.LibraryManager");
            if (libraryManager == null)
            {
                Plugin.Instance.Logger.Error("EnableDeepDelete: Failed to resolve LibraryManager type");
                return;
            }

            _deleteItem = SafeGetMethod(libraryManager, "DeleteItem",
                BindingFlags.Instance | BindingFlags.Public, 4);
        }

        protected override void Prepare(bool apply)
        {
            if (_deleteItem == null)
            {
                Plugin.Instance.Logger.Warn("EnableDeepDelete: _deleteItem is null, skipping patch");
                return;
            }
            PatchUnpatch(PatchTracker, apply, _deleteItem, prefix: nameof(DeleteItemPrefix),
                finalizer: nameof(DeleteItemFinalizer));
        }

        [HarmonyPrefix]
        private static void DeleteItemPrefix(ILibraryManager __instance, BaseItem item, DeleteOptions options,
            BaseItem parent, bool notifyParentItem, out Dictionary<string, bool?> __state)
        {
            __state = null;

            if (options.DeleteFileLocation)
            {
                var collectionFolder = options.CollectionFolders ?? __instance.GetCollectionFolders(item);
                var scope = item.GetDeletePaths(true, collectionFolder).Select(i => i.FullName).ToArray();

                __state = Plugin.LibraryApi.PrepareDeepDelete(item, scope);
            }
        }

        [HarmonyFinalizer]
        private static void DeleteItemFinalizer(Exception __exception, Dictionary<string, bool?> __state)
        {
            if (__state != null && __state.Count > 0 && __exception is null)
            {
                var localMountPaths = new HashSet<string>(__state.Where(kv => kv.Value is true).Select(kv => kv.Key));

                if (localMountPaths.Count > 0)
                {
                    Task.Run(() => Plugin.LibraryApi.ExecuteDeepDelete(localMountPaths))
                        .ContinueWith(t => { if (t.IsFaulted) ThreadLog("Error", $"EnableDeepDelete ExecuteDeepDelete failed: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}"); }, TaskScheduler.Default);
                }
            }
        }
    }
}
