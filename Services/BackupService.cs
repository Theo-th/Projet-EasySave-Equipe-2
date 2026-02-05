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
        /// <returns>Message d'erreur ou null si succès</returns>
        public string? ExecuteBackup(int jobIndex)
        {
            BackupJob? job = _configService.LoadJob(jobIndex);
            if (job == null)
                return $"Travail de sauvegarde introuvable à l'indice {jobIndex}.";

            return ExecuteBackup(job);
        }

        /// <summary>
        /// Exécute un travail de sauvegarde.
        /// </summary>
        /// <param name="job">Le travail à exécuter</param>
        /// <returns>Message d'information ou d'erreur, null si succès sans message</returns>
        public string? ExecuteBackup(BackupJob job)
        {
            try
            {
                IBackupStrategy strategy = job.Type switch
                {
                    "full" => new FullBackupStrategy(),
                    "diff" => new DifferentialBackupStrategy(),
                    _ => new FullBackupStrategy()
                };

                return strategy.ProcessBackup(job.SourceDirectory, job.TargetDirectory);
            }
            catch (Exception ex)
            {
                return $"Erreur lors de la sauvegarde : {ex.Message}";
            }
        }
    }
}