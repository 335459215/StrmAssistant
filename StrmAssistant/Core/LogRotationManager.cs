using System;
using System.IO;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.Logging;

namespace StrmAssistant.Core
{
    /// <summary>
    /// 日志轮循管理器
    /// 定期检查 Emby 日志文件大小，防止单个日志文件无限增长：
    /// - 监控当前活跃的 embyserver-*.txt 文件大小
    /// - 超过阈值时自动关闭 DebugMode 以减少日志输出
    /// - 超过硬限制时触发日志轮换（重命名当前日志 + 触发 Emby 重新创建）
    /// - 自动清理超过保留天数的旧日志文件
    /// </summary>
    public class LogRotationManager : IDisposable
    {
        private static volatile LogRotationManager _instance;
        private static readonly object _lock = new object();

        private readonly ILogger _logger;
        private readonly string _logDirectory;
        private readonly long _maxSizeBytes;
        private readonly long _hardLimitBytes;
        private readonly int _retentionDays;
        private Timer _timer;
        private int _running;
        private bool _disposed;

        /// <summary>
        /// 默认单文件大小上限：50 MB
        /// </summary>
        private const long DefaultMaxSizeMB = 50;

        /// <summary>
        /// 默认硬限制：200 MB（超过此大小强制轮换）
        /// </summary>
        private const long DefaultHardLimitMB = 200;

        /// <summary>
        /// 默认日志保留天数：7 天
        /// </summary>
        private const int DefaultRetentionDays = 7;

        /// <summary>
        /// 检查间隔：30 分钟
        /// </summary>
        private const int CheckIntervalMinutes = 30;

        private LogRotationManager(ILogger logger, string logDirectory, long maxSizeBytes, long hardLimitBytes, int retentionDays)
        {
            _logger = logger;
            _logDirectory = logDirectory;
            _maxSizeBytes = maxSizeBytes;
            _hardLimitBytes = hardLimitBytes;
            _retentionDays = retentionDays;
        }

        public static LogRotationManager Instance => _instance;

        public static void Initialize(ILogger logger, string logDirectory,
            long maxSizeMB = DefaultMaxSizeMB, long hardLimitMB = DefaultHardLimitMB,
            int retentionDays = DefaultRetentionDays)
        {
            if (_instance != null) return;

            lock (_lock)
            {
                if (_instance != null) return;

                var maxSizeBytes = maxSizeMB * 1024 * 1024;
                var hardLimitBytes = hardLimitMB * 1024 * 1024;
                _instance = new LogRotationManager(logger, logDirectory, maxSizeBytes, hardLimitBytes, retentionDays);
                _instance.Start();
            }
        }

        public static void DisposeInstance()
        {
            lock (_lock)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }

        private void Start()
        {
            if (_timer != null) return;

            var dueTime = TimeSpan.FromMinutes(1);      // 首次检查在 1 分钟后
            var interval = TimeSpan.FromMinutes(CheckIntervalMinutes);

            _timer = new Timer(OnCheck, null, dueTime, interval);
            _logger.Info($"LogRotationManager started - MaxSize: {_maxSizeBytes / 1024 / 1024}MB, " +
                        $"HardLimit: {_hardLimitBytes / 1024 / 1024}MB, " +
                        $"Retention: {_retentionDays} days, CheckInterval: {CheckIntervalMinutes} min");
        }

        private void OnCheck(object state)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;

            try
            {
                CheckCurrentLogSize();
                CleanupOldLogs();
                CleanupOldHardwareLogs();
            }
            catch (Exception ex)
            {
                _logger.Error($"LogRotationManager check failed: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        /// <summary>
        /// 检查当前活跃日志文件的大小
        /// </summary>
        private void CheckCurrentLogSize()
        {
            if (!Directory.Exists(_logDirectory)) return;

            // 查找当前活跃的 embyserver 日志文件（最新的那个）
            var logFiles = Directory.GetFiles(_logDirectory, "embyserver-*.txt")
                .Concat(Directory.GetFiles(_logDirectory, "embyserver.txt"))
                .ToList();

            foreach (var logFile in logFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(logFile);
                    if (!fileInfo.Exists) continue;

                    var sizeMB = fileInfo.Length / 1024.0 / 1024.0;
                    var fileName = Path.GetFileName(logFile);

                    if (fileInfo.Length > _hardLimitBytes)
                    {
                        _logger.Warn($"Log file {fileName} ({sizeMB:F1}MB) exceeds hard limit " +
                                     $"({_hardLimitBytes / 1024 / 1024}MB). Forcing rotation.");
                        RotateLogFile(logFile, fileName, sizeMB);
                    }
                    else if (fileInfo.Length > _maxSizeBytes)
                    {
                        // 超过软限制：关闭 DebugMode 减少日志量
                        if (Plugin.Instance != null && Plugin.Instance.DebugMode)
                        {
                            _logger.Warn($"Log file {fileName} ({sizeMB:F1}MB) exceeds soft limit " +
                                         $"({_maxSizeBytes / 1024 / 1024}MB). Disabling DebugMode to reduce log output.");
                            Plugin.Instance.DisableDebugLogging();
                        }
                        else
                        {
                            _logger.Warn($"Log file {fileName} ({sizeMB:F1}MB) exceeds soft limit " +
                                         $"({_maxSizeBytes / 1024 / 1024}MB). Consider manual rotation.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error checking log file {logFile}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 强制轮换日志文件：将当前文件重命名为带时间戳的归档文件
        /// </summary>
        private void RotateLogFile(string filePath, string fileName, double sizeMB)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var dir = Path.GetDirectoryName(filePath);
                var archiveName = $"{Path.GetFileNameWithoutExtension(fileName)}_rotated_{timestamp}.txt";
                var archivePath = Path.Combine(dir, archiveName);

                File.Move(filePath, archivePath);
                _logger.Warn($"Log file rotated: {fileName} ({sizeMB:F1}MB) → {archiveName}");

                // 创建新的空日志文件，让 Emby 继续写入
                // Emby 会自动检测并创建新文件，但先创建一个空的确保不丢失日志
                File.WriteAllText(filePath, $"[{DateTime.Now:O}] Log rotated by StrmAssistant LogRotationManager. Previous file archived as {archiveName}\r\n");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to rotate log file {fileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理超过保留天数的旧 embyserver 日志文件
        /// </summary>
        private void CleanupOldLogs()
        {
            if (!Directory.Exists(_logDirectory)) return;

            var cutoff = DateTime.Now.AddDays(-_retentionDays);

            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "embyserver-*.txt");
                foreach (var file in logFiles)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.Exists && info.LastWriteTime < cutoff)
                        {
                            var name = Path.GetFileName(file);
                            var sizeKB = info.Length / 1024.0;
                            info.Delete();
                            _logger.Info($"Deleted old log: {name} ({sizeKB:F0}KB, older than {_retentionDays} days)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Failed to delete log file {file}: {ex.Message}");
                    }
                }

                // 也清理轮换产生的归档文件
                var rotatedFiles = Directory.GetFiles(_logDirectory, "embyserver-*_rotated_*.txt");
                foreach (var file in rotatedFiles)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.Exists && info.LastWriteTime < cutoff)
                        {
                            info.Delete();
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error during log cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理超过保留天数的旧硬件检测日志文件
        /// </summary>
        private void CleanupOldHardwareLogs()
        {
            if (!Directory.Exists(_logDirectory)) return;

            var cutoff = DateTime.Now.AddDays(-_retentionDays);

            try
            {
                var hwLogFiles = Directory.GetFiles(_logDirectory, "hardware_detection-*.txt");
                foreach (var file in hwLogFiles)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.Exists && info.LastWriteTime < cutoff)
                        {
                            info.Delete();
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer?.Dispose();
            _timer = null;
        }
    }
}
