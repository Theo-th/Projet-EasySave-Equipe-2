using EasySave.Core.Models;

namespace EasySave.Core.Interfaces
{
    
    /// Interface for the backup job execution service.

    public interface IBackupService
    {
        
        /// Event triggered when a backup job's progress changes.

        event Action<BackupJobState>? OnProgressChanged;

        
        /// Executes a backup job by its index.

        /// <param name="jobIndices">List of job indices (0-based)</param>
        /// <returns>Error message or null if successful</returns>
        string? ExecuteBackup(List<int> jobIndices);

        void ChangeLogFormat(LogType logType);
        
        
        /// Updates the logs directory path.

        void UpdateLogsDirectory(string newLogsDirectory);
    }

}