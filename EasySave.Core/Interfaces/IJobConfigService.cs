using EasySave.Core.Models;

namespace EasySave.Core.Interfaces
{
    
    /// <summary>
    /// Interface for the backup job configuration service.
    /// </summary>
    public interface IJobConfigService
    {
        /// <summary>
        /// Gets all backup jobs.
        /// </summary>

        List<BackupJob> GetAllJobs();
        /// <summary>
        /// Gets a backup job by its index.
        /// </summary>
        BackupJob? GetJob(int index);
        /// <summary>
        /// Gets the total number of backup jobs.
        /// </summary>
        int GetJobCount();
        /// <summary>
        /// Creates a new backup job.
        /// </summary>

        (bool Success, string? ErrorMessage) CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type);
        /// <summary>
        /// Removes a backup job by its index.
        /// </summary>

        bool RemoveJob(int index);
        
        
        /// <summary>
        /// Updates the configuration file path.
        /// </summary>

        void UpdateConfigPath(string newConfigPath);
    }
}