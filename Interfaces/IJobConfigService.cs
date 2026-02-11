using Projet_EasySave.Models;

namespace Projet_EasySave.Interfaces
{
    /// <summary>
    /// Interface pour le service de configuration des travaux de sauvegarde.
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