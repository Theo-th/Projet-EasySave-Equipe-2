using EasyLog;
using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Manages logging operations for backup services.
    /// Handles both local and remote logging with thread-safe operations.
    /// </summary>
    public class BackupLogManager
    {
        private BaseLog _logger = null!;
        private string _logDirectory;
        private LogTarget _currentLogTarget;
        private readonly object _logLock = new();

        /// <summary>
        /// Initializes a new instance of <see cref="BackupLogManager"/> with the specified log type and directory.
        /// </summary>
        public BackupLogManager(LogType logType, string? logDirectory = null)
        {
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _currentLogTarget = LogTarget.Both;
            ChangeLogFormat(logType);
        }

        /// <summary>
        /// Sets the log output target (Local, Server, or Both).
        /// </summary>
        public void SetLogTarget(LogTarget target) => _currentLogTarget = target;

        /// <summary>
        /// Changes the log serialization format (JSON or XML).
        /// </summary>
        public void ChangeLogFormat(LogType logType)
        {
            _logger = logType == LogType.JSON
                ? new JsonLog(_logDirectory)
                : new XmlLog(_logDirectory);
        }

        /// <summary>
        /// Updates the directory where log files are written.
        /// </summary>
        public void UpdateLogsDirectory(string newLogsDirectory)
        {
            _logDirectory = newLogsDirectory;
            ChangeLogFormat(_logger is JsonLog ? LogType.JSON : LogType.XML);
        }

        /// <summary>
        /// Writes a log record to the configured target(s) (local, remote, or both).
        /// </summary>
        public void WriteLog(Record record)
        {
            if (_currentLogTarget == LogTarget.Local || _currentLogTarget == LogTarget.Both)
            {
                lock (_logLock) { _logger.WriteLog(record); }
            }

            if (_currentLogTarget == LogTarget.Server || _currentLogTarget == LogTarget.Both)
                _ = RemoteLogService.SendLogAsync(record);
        }

        /// <summary>
        /// Converts a local file path to UNC format (network path).
        /// </summary>
        public static string GetUncPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith(@"\\")) return path;
            if (path.Length >= 2 && path[1] == ':')
                return $@"\\{Environment.MachineName}\{path[0]}${path.Substring(2)}";
            return path;
        }
    }
}
