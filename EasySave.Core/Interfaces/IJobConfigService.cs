using EasySave.Core.Models;

namespace EasySave.Core.Interfaces
{
    /// <summary>
    /// Interface for the backup job configuration service.
    /// </summary>
    public interface IJobConfigService
    {
        List<BackupJob> GetAllJobs();
        BackupJob? GetJob(int index);
        int GetJobCount();
        (bool Success, string? ErrorMessage) CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type);
        bool RemoveJob(int index);
    }
}