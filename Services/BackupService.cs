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
            // Compter les fichiers à sauvegarder
            int totalFiles = CountFiles(job.SourceDirectory);
            long totalSize = CalculateTotalSize(job.SourceDirectory);

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
            _stateRepository.UpdateState(new List<BackupJobState> { jobState });

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

                return $"Erreur lors de la sauvegarde : {ex.Message}";
            }
        }

        /// <summary>
        /// Compte le nombre total de fichiers dans un répertoire et ses sous-répertoires
        /// </summary>
        private int CountFiles(string directory)
        {
            try
            {
                return Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Calcule la taille totale des fichiers dans un répertoire et ses sous-répertoires
        /// </summary>
        private long CalculateTotalSize(string directory)
        {
            try
            {
                return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            }
            catch
            {
                return 0;
            }
        }
    }
}