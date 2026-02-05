using Projet_EasySave.EasyLog;
using Projet_EasySave.Models;
using Projet_EasySave.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            return ExecuteBackup(job, jobIndex);
        }

        /// <summary>
        /// Ex�cute un travail de sauvegarde.
        /// </summary>
        /// <param name="job">Le travail � ex�cuter</param>
        /// <param name="jobIndex">Indice du travail</param>
        /// <returns>Message d'information ou d'erreur, null si succ�s sans message</returns>
        private string? ExecuteBackup(BackupJob job, int jobIndex = 0)
        {
            // Compter les fichiers et calculer la taille en un seul parcours
            var (totalFiles, totalSize) = GetFilesInfo(job.SourceDirectory);

            // Créer l'état initial (Actif)
            var jobState = new BackupJobState
            {
                Id = jobIndex + 1,
                Name = job.Name,
                SourcePath = job.SourceDirectory,
                TargetPath = job.TargetDirectory,
                Type = job.Type == "full" ? BackupType.Complete : BackupType.Differential,
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
            UpdateJobState(jobState, BackupState.Active);

            try
            {
                IBackupStrategy strategy = job.Type switch
                {
                    "full" => new FullBackupStrategy(),
                    "diff" => new DifferentialBackupStrategy(),
                    _ => new FullBackupStrategy()
                };

                var result = strategy.ProcessBackup(job.SourceDirectory, job.TargetDirectory, job.Name, _log);

                // Mettre à jour l'état (sauvegarde terminée)
                UpdateJobState(jobState, BackupState.Completed, 0, 0);

                return result;
            }
            catch (Exception ex)
            {
                // Mettre à jour l'état (erreur)
                UpdateJobState(jobState, BackupState.Error);

                return $"Erreur lors de la sauvegarde : {ex.Message}";
            }
        }

        /// <summary>
        /// Obtient le nombre de fichiers et la taille totale en un seul parcours
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

        /// <summary>
        /// Met à jour l'état d'un travail de sauvegarde
        /// </summary>
        private void UpdateJobState(BackupJobState jobState, BackupState state, 
            int remainingFiles = -1, long remainingSize = -1)
        {
            jobState.State = state;
            jobState.LastActionTimestamp = DateTime.Now;
            if (remainingFiles >= 0) jobState.RemainingFiles = remainingFiles;
            if (remainingSize >= 0) jobState.RemainingSize = remainingSize;
            _stateRepository.UpdateState(new List<BackupJobState> { jobState });
        }
    }
}