using Projet_EasySave.EasyLog;
using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;
using Projet_EasySave.Properties;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service d'exécution des travaux de sauvegarde.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly JsonLog _log;
        private readonly IBackupStateRepository _stateRepository;

        public BackupService(IJobConfigService configService, JsonLog log, IBackupStateRepository stateRepository)
        {
            _configService = configService;
            _log = log;
            _stateRepository = stateRepository;
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
                return string.Format(Lang.JobIndexNotFound, jobIndex);

            return ExecuteBackup(job, jobIndex);
        }

        /// <summary>
        /// Exécute un travail de sauvegarde.
        /// </summary>
        /// <param name="job">Le travail à exécuter</param>
        /// <param name="jobIndex">Indice du travail</param>
        /// <returns>Message d'information ou d'erreur, null si succès sans message</returns>
        private string? ExecuteBackup(BackupJob job, int jobIndex = 0)
        {
            // Compter les fichiers et calculer la taille en un seul parcours (optimisé)
            var (totalFiles, totalSize) = GetFilesInfo(job.SourceDirectory);

            // Créer l'état initial (Actif)
            var jobState = new BackupJobState
            {
                Id = jobIndex + 1,
                Name = job.Name,
                SourcePath = job.SourceDirectory,
                TargetPath = job.TargetDirectory,
                Type = job.Type == BackupType.Complete ? BackupType.Complete : BackupType.Differential,
                State = BackupState.Active,
                LastActionTimestamp = DateTime.Now,
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = totalFiles,
                RemainingSize = totalSize,
                CurrentSourceFile = "",
                CurrentTargetFile = ""
            };

            // Mettre à jour l'état (début de sauvegarde)
            _stateRepository.UpdateState(new List<BackupJobState> { jobState });

            try
            {
                IBackupStrategy strategy = job.Type switch
                {
                    BackupType.Complete => new FullBackupStrategy(),
                    BackupType.Differential => new DifferentialBackupStrategy(),
                    _ => new FullBackupStrategy()
                };

                var result = strategy.ProcessBackup(job.SourceDirectory, job.TargetDirectory, job.Name, _log);

                // Mettre à jour l'état (sauvegarde terminée)
                jobState.State = BackupState.Completed;
                jobState.RemainingFiles = 0;
                jobState.RemainingSize = 0;
                jobState.LastActionTimestamp = DateTime.Now;
                _stateRepository.UpdateState(new List<BackupJobState> { jobState });

                return result;
            }
            catch (Exception ex)
            {
                // Mettre à jour l'état (erreur)
                jobState.State = BackupState.Error;
                jobState.LastActionTimestamp = DateTime.Now;
                _stateRepository.UpdateState(new List<BackupJobState> { jobState });

                return string.Format(Lang.SaveErrorWithException, ex.Message);
            }
        }

        /// <summary>
        /// Obtient le nombre de fichiers et la taille totale en un seul parcours (optimisé)
        /// </summary>
        private (int count, long size) GetFilesInfo(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                return (files.Length, files.Sum(f => new FileInfo(f).Length));
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}