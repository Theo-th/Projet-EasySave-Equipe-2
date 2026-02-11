using EasySave.Models;
using EasyLog;
using Projet_EasySave.Models;
using Projet_EasySave.Interfaces;
using Projet_EasySave.Services.Strategies;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service for executing and managing backup operations.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly BaseLog _logger;

        public BackupService(IJobConfigService configService, LogType logType)
        {
            _configService = configService;

            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            _logger = logType switch
            {
                LogType.JSON => new JsonLog(logDirectory),
                LogType.XML => new XmlLog(logDirectory),
                _ => new JsonLog(logDirectory)
            };
        }

        /// <summary>
        /// Executes a backup job by its index.
        /// </summary>
        /// <returns>Formatted backup status message or error message</returns>
        public string? ExecuteBackup(List<int> jobIndices)
        {
            if (jobIndices == null || jobIndices.Count == 0)
            {
                return "Aucun travail de sauvegarde spécifié.";
            }

            var allJobs = _configService.GetAllJobs();
            var results = new List<string>();

            foreach (int index in jobIndices)
            {
                if (index < 0 || index >= allJobs.Count)
                {
                    results.Add($"Erreur : L'indice {index} est invalide.");
                    continue;
                }

                BackupJob job = allJobs[index];

                // Créer la stratégie appropriée selon le type de sauvegarde
                BackupStrategy strategy = CreateBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Type, job.Name);
                
                // Exécuter la sauvegarde
                var (success, errorMessage) = strategy.Execute();

                if (success)
                {
                    results.Add($"Sauvegarde '{job.Name}' terminée avec succès.");
                }
                else
                {
                    results.Add($"Erreur lors de la sauvegarde '{job.Name}' : {errorMessage}");
                }
            }

            return string.Join("\n", results);
        }

        /// <summary>
        /// Crée la stratégie de sauvegarde appropriée selon le type.
        /// </summary>
        private BackupStrategy CreateBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName)
        {
            return backupType switch
            {
                BackupType.Complete => new FullBackupStrategy(sourceDirectory, targetDirectory, backupType, jobName, _logger),
                BackupType.Differential => new DifferentialBackupStrategy(sourceDirectory, targetDirectory, backupType, jobName, _logger),
                _ => throw new InvalidOperationException($"Type de sauvegarde non supporté : {backupType}")
            };
        }
    }
}