using EasySave.Core.Models;

namespace EasySave.Core.Interfaces
{
    /// <summary>
    /// Interface du service de sauvegarde.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Event triggered when a backup job's progress changes.
        /// </summary>
        event Action<BackupJobState>? OnProgressChanged;

        /// <summary>
        /// Event triggered when a watched business process is detected during backup.
        /// </summary>
        event Action<string>? OnBusinessProcessDetected;

        /// <summary>
        /// Executes a backup job by its index.
        /// </summary>
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

        /// <summary>
        /// Changes the log format.
        /// </summary>
        /// <param name="logType">The new log format.</param>
        void ChangeLogFormat(LogType logType);

        /// <summary>
        /// Updates the logs directory path.
        /// </summary>
        /// <param name="newLogsDirectory">The new logs directory path.</param>
        void UpdateLogsDirectory(string newLogsDirectory);

        /// <summary>
        /// Pauses a specific job.
        /// </summary>
        /// <param name="jobName">The name of the job to pause.</param>
        void PauseJob(string jobName);

        /// <summary>
        /// Resumes a specific job.
        /// </summary>
        /// <param name="jobName">The name of the job to resume.</param>
        void ResumeJob(string jobName);

        /// <summary>
        /// Stops a specific job.
        /// </summary>
        /// <param name="jobName">The name of the job to stop.</param>
        void StopJob(string jobName);
    }
}