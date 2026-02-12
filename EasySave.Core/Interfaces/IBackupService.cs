using EasySave.Core.Models;

namespace EasySave.Core.Interfaces
{
    /// <summary>
    /// Interface for the backup job execution service.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Event triggered when a backup job's progress changes.
        /// </summary>
        event Action<BackupJobState>? OnProgressChanged;

        /// <summary>
        /// Executes a backup job by its index.
        /// </summary>
        /// <param name="jobIndices">List of job indices (0-based)</param>
        /// <returns>Error message or null if successful</returns>
        string? ExecuteBackup(List<int> jobIndices);

        void ChangeLogFormat(LogType logType);
        
        /// <summary>
        /// Updates the logs directory path.
        /// </summary>
        void UpdateLogsDirectory(string newLogsDirectory);
    }

}