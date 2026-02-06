using Projet_EasySave.Models;

namespace Projet_EasySave.Interfaces
{
    /// <summary>
    /// Interface pour le service de configuration des travaux de sauvegarde.
    /// </summary>
    public interface IJobConfigService
    {
        List<BackupJob> LoadAllJobs();
        BackupJob? LoadJob(int index);
        (bool Success, string? ErrorMessage) CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type);
        bool SaveJob();
        bool RemoveJob(int index);
        int GetJobCount();
        List<BackupJob> GetAllJobs();
    }
}