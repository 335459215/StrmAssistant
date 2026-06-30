using HarmonyLib;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static StrmAssistant.Mod.PatchManager;
using static StrmAssistant.Options.MediaInfoExtractOptions;

namespace StrmAssistant.Mod
{
    public class ChapterChangeTracker : PatchBase<ChapterChangeTracker>
    {
        private static MethodInfo _saveChapters;
        private static MethodInfo _deleteChapters;
        private static MethodInfo _onFailedToFindIntro;

        private static readonly AsyncLocal<long> BypassItem = new AsyncLocal<long>();

        public ChapterChangeTracker()
        {
            Initialize();

            if (Plugin.Instance.MediaInfoExtractStore.GetOptions().PersistMediaInfoMode !=
                PersistMediaInfoOption.None.ToString())
            {
                Patch();
            }
        }

        protected override void OnInitialize()
        {
            var embyServerImplementationsAssembly = Assembly.Load("Emby.Server.Implementations");
            var sqliteItemRepository =
                embyServerImplementationsAssembly.GetType("Emby.Server.Implementations.Data.SqliteItemRepository");
            _saveChapters = SafeGetMethod(sqliteItemRepository, "SaveChapters",
                BindingFlags.Instance | BindingFlags.Public, 3);
            _deleteChapters =
                SafeGetMethod(sqliteItemRepository, "DeleteChapters", BindingFlags.Instance | BindingFlags.Public);

            var embyProviders = Assembly.Load("Emby.Providers");
            var audioFingerprintManager = embyProviders.GetType("Emby.Providers.Markers.AudioFingerprintManager");
            _onFailedToFindIntro = SafeGetMethod(audioFingerprintManager, "OnFailedToFindIntro",
                BindingFlags.NonPublic | BindingFlags.Static);
        }

        protected override void Prepare(bool apply)
        {
            if (Plugin.Instance.MediaInfoExtractStore.GetOptions().IsModSupported)
            {
                PatchUnpatch(PatchTracker, apply, _saveChapters, postfix: nameof(SaveChaptersPostfix));
                //PatchUnpatch(PatchTracker, apply, _deleteChapters, postfix: nameof(DeleteChaptersPostfix));
                PatchUnpatch(PatchTracker, apply, _onFailedToFindIntro, postfix: nameof(OnFailedToFindIntroPostfix));
            }
        }

        public static void BypassInstance(BaseItem item)
        {
            BypassItem.Value = item.InternalId;
        }

        [HarmonyPostfix]
        private static void SaveChaptersPostfix(long itemId, bool clearExtractionFailureResult,
            List<ChapterInfo> chapters)
        {
            if (chapters.Count == 0) return;

            if (BypassItem.Value != 0 && BypassItem.Value == itemId) return;

            Plugin.MediaInfoApi.SerializeMediaInfo(itemId, null, true, "Save Chapters")
                .ContinueWith(t => { if (t.IsFaulted) ThreadLog("Error", $"ChapterChangeTracker SaveChapters failed: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}"); }, TaskScheduler.Default);
        }

        [HarmonyPostfix]
        private static void DeleteChaptersPostfix(long itemId, MarkerType[] markerTypes)
        {
            if (BypassItem.Value != 0 && BypassItem.Value == itemId) return;

            Plugin.MediaInfoApi.SerializeMediaInfo(itemId, null, true, "Delete Chapters")
                .ContinueWith(t => { if (t.IsFaulted) ThreadLog("Error", $"ChapterChangeTracker DeleteChapters failed: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}"); }, TaskScheduler.Default);
        }

        [HarmonyPostfix]
        private static void OnFailedToFindIntroPostfix(Episode episode, bool __runOriginal)
        {
            if (__runOriginal)
            {
                Plugin.MediaInfoApi.SerializeMediaInfo(episode.InternalId, null, true,
                    "Zero Fingerprint Confidence")
                    .ContinueWith(t => { if (t.IsFaulted) ThreadLog("Error", $"ChapterChangeTracker OnFailedToFindIntro failed: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}"); }, TaskScheduler.Default);
            }
        }
    }
}
