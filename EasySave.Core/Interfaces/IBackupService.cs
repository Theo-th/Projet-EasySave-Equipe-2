using EasySave.Core.Models;

namespace EasySave.Core.Interfaces
{
    
    /// Interface for the backup job execution service.

    public interface IBackupService
    {
        
        /// Event triggered when a backup job's progress changes.

        event Action<BackupJobState>? OnProgressChanged;

        /// Event triggered when a watched business process is detected during backup.
        event Action<string>? OnBusinessProcessDetected;

        
        /// Executes a backup job by its index.

        /// <param name="jobIndices">List of job indices (0-based)</param>
        /// <returns>Error message or null if successful</returns>
        string? ExecuteBackup(List<int> jobIndices);

        /// <summary>
        /// Pauses the currently active backup.
        /// </summary>
        void PauseBackup();

        /// <summary>
        /// Resumes the currently paused backup.
        /// </summary>
        void ResumeBackup();

        /// <summary>
        /// Stops (cancels) the currently active backup.
        /// </summary>
        void StopBackup();

        void ChangeLogFormat(LogType logType);
        
        
        /// Updates the logs directory path.

        void UpdateLogsDirectory(string newLogsDirectory);
    }

}