# Harmony补丁热重载后丢失的问题分析与修复方案

## 问题分析

###补丁生命周期概述

1. **初始化阶段** (`PatchManager.Initialize()`):
   - 创建 `HarmonyMod = new Harmony("emby.mod")` 
   - 实例化所有补丁类（EnableImageCapture, MergeMultiVersion等）
   - 每个补丁类构造函数中调用 `Initialize()` → `OnInitialize()`
   - 根据配置选项调用 `Patch()` → `Prepare(true)` → `PatchUnpatch(...)`

2. **卸载阶段** (`CleanupResources()`):
   - 仅清理事件订阅、释放资源、清缓存
   - **不卸载Harmony补丁** — 没有 `UnpatchAll()` 调用

3. **热重载流程**:
   - Emby卸载旧DLL → 调用 `OnUninstalling()` → `CleanupResources(true)`
   - Emby加载新DLL → 调用Plugin构造函数 → `PatchManager.Initialize()`

### 根本原因：5个关键问题

#### 问题1：`_isInitialized` 标志阻止重新初始化
```csharp
public static void Initialize()
{
    if (_isInitialized)  // ← 第二次调用时直接return！
    {
        Plugin.Instance.Logger.Warn("PatchManager already initialized");
        return;
    }
```
`_isInitialized` 是 `static bool`，在AppDomain中跨插件重载持久化。
当新插件DLL加载后调用 `Initialize()`，由于旧实例已设置 `_isInitialized = true`，
**整个初始化被跳过** — 不创建新Harmony实例，不重新实例化补丁类，不重新应用补丁。

这是**最核心的问题**。

#### 问题2：`HarmonyMod` 静态引用指向旧的Harmony实例
旧DLL创建的 `HarmonyMod` 实例引用的是旧DLL的程序集。
即使 `_isInitialized` 检查通过，如果不清空 `HarmonyMod`，旧的Harmony实例
可能无法正确patch新加载的方法（方法的DeclaringType可能来自不同的程序集版本）。

#### 问题3：`PatchTrackerList` 和缓存未清理
`PatchTrackerList` 是 `static readonly List`，在构造函数中 `new PatchTracker()` 
会把自己加入此列表。热重载时：
- 旧补丁类的tracker仍在列表中，但其引用的方法来自旧DLL
- 新补丁类的tracker被追加到列表
- `HarmonyMethodCache` 和 `MethodInfoCache` 缓存了旧DLL的MethodInfo

#### 问题4：`FallbackPatchApproach` 状态残留
在 `PatchUnpatch()` 中，如果补丁失败，`FallbackPatchApproach` 会被设为 `Reflection` 或 `None`。
热重载时，这些状态从旧tracker继承，即使新DLL可能能够成功应用Harmony补丁，
也被跳过了：
```csharp
if (tracker.FallbackPatchApproach != PatchApproach.Harmony) return false;  // ← 直接返回
```

#### 问题5：补丁实例的实例字段未重置
如 `EnableImageCapture._isShortcutPatchUsageCount`（static int），
`MergeMultiVersion._isEligibleForMultiVersion`（static MethodInfo）等，
这些在热重载后残留旧值，可能导致空引用或错误行为。

### v3.0.0.37 变更说明
版本日志提到修复了"插件更新后不重启魔改功能可能会失效的问题"，
说明开发者已意识到此问题但当前修复可能不完善。

---

## 修复方案

### 核心策略：`Initialize()` 需要支持重入（re-entrant）

当 `_isInitialized = true` 时，不应直接返回，而应：
1. 先完整卸载旧补丁
2. 清理所有静态状态
3. 重新创建Harmony实例
4. 重新实例化并应用所有补丁

### 具体代码修改

#### 修改1：PatchManager.cs - 添加 CleanupPatches 方法

在 `ClearCaches()` 方法之后添加完整的清理方法：

```csharp
/// <summary>
/// 完整清理所有补丁和静态状态，用于插件热重载
/// </summary>
public static void CleanupPatches()
{
    // 1. 卸载所有Harmony补丁
    if (HarmonyMod != null)
    {
        try
        {
            HarmonyMod.UnpatchAll(HarmonyMod.Id);
            Plugin.Instance.Logger.Info("All Harmony patches unpatched");
        }
        catch (Exception e)
        {
            Plugin.Instance.Logger.Error($"Failed to unpatch all: {e.Message}");
        }
    }

    // 2. 清空补丁追踪列表
    PatchTrackerList.Clear();

    // 3. 清空缓存
    HarmonyMethodCache.Clear();
    MethodInfoCache.Clear();

    // 4. 重置状态标志
    _isInitialized = false;
    _lastModSuccessStatus = null;
    _lastStatusLog = null;
    HarmonyMod = null;

    // 5. 清空补丁实例引用
    EnableImageCapture = null;
    EnhanceChineseSearch = null;
    MergeMultiVersion = null;
    ExclusiveExtract = null;
    ChineseMovieDb = null;
    ChineseTvdb = null;
    EnhanceMovieDbPerson = null;
    AltMovieDbConfig = null;
    EnableProxyServer = null;
    PreferOriginalPoster = null;
    UnlockIntroSkip = null;
    PinyinSortName = null;
    EnhanceNfoMetadata = null;
    HidePersonNoImage = null;
    EnforceLibraryOrder = null;
    BeautifyMissingMetadata = null;
    EnhanceMissingEpisodes = null;
    ChapterChangeTracker = null;
    MovieDbEpisodeGroup = null;
    OptimizeMovieDbEpisodeScraping = null;
    NoBoxsetsAutoCreation = null;
    EnhanceNotificationSystem = null;
    EnableDeepDelete = null;
    SuppressPluginUpdate = null;

    Plugin.Instance.Logger.Info("PatchManager cleanup completed - ready for re-initialization");
}
```

#### 修改2：PatchManager.cs - 修改 Initialize() 支持热重载

将 `if (_isInitialized) return` 改为调用清理并重新初始化：

```csharp
public static void Initialize()
{
    if (_isInitialized)
    {
        Plugin.Instance.Logger.Info("PatchManager re-initializing (plugin hot-reload detected)");
        CleanupPatches();
    }
    
    try
    {
        // ... 原有初始化代码不变 ...
    }
```

#### 修改3：Plugin.cs - CleanupResources 时调用 CleanupPatches

在 `CleanupResources()` 中添加补丁清理：

```csharp
PatchManager.CleanupPatches();  // 替换 PatchManager.ClearCaches();
```

---

## 补丁实施建议

### 最高优先级：修改 Initialize() 的 _isInitialized 检查逻辑

这是root cause。如果不修改此处，所有其他修改都无法生效。

### 注意事项

1. **Harmony ID 一致性**：`new Harmony("emby.mod")` 使用固定ID，`UnpatchAll(HarmonyMod.Id)` 
   可以正确匹配并卸载所有此ID注册的补丁。

2. **线程安全**：热重载期间Emby可能在调用被patch的方法，需要考虑竞态条件。
   建议在卸载和重新应用之间加锁或使用标志位让补丁方法快速返回。

3. **static字段重置**：每个补丁类的static字段（如 `MergeMultiVersion._isEligibleForMultiVersion`）
   在新实例的 `OnInitialize()` 中会被重新解析，所以不需要手动清理。
   但像 `_isShortcutPatchUsageCount` 这样的计数器可能需要重置。

4. **ReversePatch**：逆向补丁不需要显式unpatch（它是注入到stub方法的），
   `UnpatchAll` 也不会影响反向补丁。重新应用时会重新创建。

5. **Reflection模式的补丁**：这些补丁不通过Harmony注册，`UnpatchAll` 不会影响它们。
   它们通过事件拦截或方法替换工作，需要在各自类中处理清理。
