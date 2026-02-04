using Projet_EasySave.Models;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service d'exécution des travaux de sauvegarde.
    /// </summary>
    public class BackupService
    {
        private readonly JobConfigService _configService;

        public BackupService(JobConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Exécute un travail de sauvegarde par son indice.
        /// </summary>
        /// <param name="jobIndex">Indice du travail (0-based)</param>
        /// <returns>true si l'exécution a réussi, false sinon</returns>
        public bool ExecuteBackup(int jobIndex)
        {
            BackupJob? job = _configService.LoadJob(jobIndex);
            if (job == null)
                return false;

            return ExecuteBackup(job);
        }

        /// <summary>
        /// Exécute un travail de sauvegarde.
        /// </summary>
        /// <param name="job">Le travail à exécuter</param>
        /// <returns>true si l'exécution a réussi, false sinon</returns>
        public bool ExecuteBackup(BackupJob job)
        {
            try
            {
                IBackupStrategy strategy = job.Type.ToLower() switch
                {
                    "complete" or "complète" => new FullBackupStrategy(),
                    "differential" or "différentielle" => new DifferentialBackupStrategy(),
                    _ => new FullBackupStrategy()
                };

                strategy.ProcessBackup(job.SourceDirectory, job.TargetDirectory);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
                return false;
            }
        }
    }
}