using Projet_EasySave.EasyLog;
using Projet_EasySave.Models;
using Projet_EasySave.Interfaces;
using System.Collections.Generic;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service d'exécution des travaux de sauvegarde.
    /// </summary>
    public class BackupService
    {
        private readonly JobConfigService _configService;
        private readonly JsonLog _log;
        private readonly IBackupStateRepository _stateRepository;

        public BackupService(JobConfigService configService, JsonLog log, IBackupStateRepository stateRepository)
        {
            _configService = configService;
            _log = log;
            _stateRepository = stateRepository;
        }

        /// <summary>
        /// Ex�cute un travail de sauvegarde par son indice.
        /// </summary>
        /// <param name="jobIndex">Indice du travail (0-based)</param>
        /// <returns>Message d'erreur ou null si succ�s</returns>
        public string? ExecuteBackup(int jobIndex)
        {
            BackupJob? job = _configService.LoadJob(jobIndex);
            if (job == null)
                return $"Travail de sauvegarde introuvable � l'indice {jobIndex}.";

            return ExecuteBackup(job);
        }

        /// <summary>
        /// Ex�cute un travail de sauvegarde.
        /// </summary>
        /// <param name="job">Le travail � ex�cuter</param>
        /// <returns>Message d'information ou d'erreur, null si succ�s sans message</returns>
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

                return strategy.ProcessBackup(job.SourceDirectory, job.TargetDirectory, job.Name, _log);
            }
            catch (Exception ex)
            {
                return $"Erreur lors de la sauvegarde : {ex.Message}";
            }
        }
    }
}