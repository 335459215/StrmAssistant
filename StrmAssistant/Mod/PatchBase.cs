using System;
using System.Linq;
using System.Reflection;
using static StrmAssistant.Mod.PatchManager;

namespace StrmAssistant.Mod
{
    /// <summary>
    /// 线程安全日志工具：在独立 Thread / Task.Run / ContinueWith 中 Plugin.Instance.Logger 可能不可用，
    /// 使用 Console.WriteLine 输出到 stdout（docker logs 捕获），同时 try-catch 包装 Logger。
    /// </summary>
    internal static class ThreadLogHelper
    {
        internal static void Log(string level, string message)
        {
            Console.WriteLine($"Strm Assistant: [{level}] {message}");
            try
            {
                switch (level)
                {
                    case "Error": Plugin.Instance.Logger.Error(message); break;
                    case "Warn": Plugin.Instance.Logger.Warn(message); break;
                    default: Plugin.Instance.Logger.Info(message); break;
                }
            }
            catch { /* Logger not available in thread context — Console.WriteLine is sufficient */ }
        }
    }

    public abstract class PatchBase<T> where T : PatchBase<T>
    {
        public PatchTracker PatchTracker;

        public static T Instance { get; private set; }

        protected PatchBase()
        {
            Instance = (T)this;
            PatchTracker = new PatchTracker(typeof(T), PatchApproach.Harmony);
        }

        /// <summary>
        /// 清理静态 Instance 引用，用于插件热重载时防止旧实例泄漏
        /// </summary>
        public static void ClearInstance()
        {
            Instance = null;
        }

        /// <summary>
        /// 线程安全日志：在独立 Thread / Task.Run / ContinueWith 中 Plugin.Instance.Logger 可能不可用，
        /// 使用 Console.WriteLine 输出到 stdout（docker logs 捕获），同时 try-catch 包装 Logger。
        /// internal static 允许同一 assembly 内所有类访问。
        /// </summary>
        protected static void ThreadLog(string level, string message) => ThreadLogHelper.Log(level, message);

        protected void Initialize()
        {
            PatchTracker.Status = PatchStatus.Initializing;
            
            try
            {
                OnInitialize();
                PatchTracker.Status = PatchStatus.Initialized;
                PatchTracker.InitializedAt = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                PatchTracker.Status = PatchStatus.Failed;
                PatchTracker.AddError($"Initialization failed: {e.Message}");
                
                if (Plugin.Instance.DebugMode)
                {
                    Plugin.Instance.Logger.Debug(e.Message);
                    Plugin.Instance.Logger.Debug(e.StackTrace);
                }

                Plugin.Instance.Logger.Warn($"{PatchTracker.PatchType.Name} Init Failed: {e.Message}");
                PatchTracker.FallbackPatchApproach = PatchApproach.None;
            }

            if (PatchTracker.FallbackPatchApproach == PatchApproach.None)
            {
                PatchTracker.Status = PatchStatus.NotSupported;
                return;
            }

            if (HarmonyMod is null)
            {
                PatchTracker.FallbackPatchApproach = PatchApproach.Reflection;
                Plugin.Instance.Logger.Debug($"{PatchTracker.PatchType.Name} using Reflection (Harmony unavailable)");
            }
        }

        protected abstract void OnInitialize();

        protected abstract void Prepare(bool apply);

        public void Patch() => Prepare(true);

        public void Unpatch() => Prepare(false);

        /// <summary>
        /// 安全获取方法，防止 AmbiguousMatchException。
        /// 可选指定参数数量做精确匹配。
        /// </summary>
        protected static MethodInfo SafeGetMethod(Type type, string methodName, BindingFlags bindingFlags, int? expectedParamCount = null)
        {
            if (type == null) return null;
            try
            {
                var method = type.GetMethod(methodName, bindingFlags);
                if (method != null && expectedParamCount.HasValue && method.GetParameters().Length != expectedParamCount.Value)
                    method = null;
                if (method != null) return method;
            }
            catch (AmbiguousMatchException)
            {
                // Fall through to GetMethods approach
            }

            return type.GetMethods(bindingFlags)
                .FirstOrDefault(m => m.Name == methodName && 
                    (!expectedParamCount.HasValue || m.GetParameters().Length == expectedParamCount.Value));
        }

        /// <summary>
        /// 安全获取方法（默认 BindingFlags.Public | BindingFlags.Instance）。
        /// </summary>
        protected static MethodInfo SafeGetMethod(Type type, string methodName, int? expectedParamCount = null)
            => SafeGetMethod(type, methodName, BindingFlags.Public | BindingFlags.Instance, expectedParamCount);
    }
}
