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
        private readonly IBackupStateRepository _stateRepository;
        private readonly BaseLog _logger;

        /// <summary>
        /// Événement déclenché à chaque changement de progression d'un travail de sauvegarde.
        /// </summary>
        public event Action<BackupJobState>? OnProgressChanged;

        public BackupService(IJobConfigService configService, IBackupStateRepository stateRepository, LogType logType)
        {
            _configService = configService;
            _stateRepository = stateRepository;

            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            _logger = logType switch
            {
                LogType.JSON => new JsonLog(logDirectory),
                LogType.XML => new XmlLog(logDirectory),
                _ => new JsonLog(logDirectory)
            };
        }

        /// <summary>
        /// Executes backup jobs by their indices.
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
            var states = new List<BackupJobState>();

            foreach (int index in jobIndices)
            {
                if (index < 0 || index >= allJobs.Count)
                {
                    results.Add($"Erreur : L'indice {index} est invalide.");
                    continue;
                }

                BackupJob job = allJobs[index];

                // Initialiser l'état du travail
                var jobState = new BackupJobState
                {
                    Id = index,
                    Name = job.Name,
                    SourcePath = job.SourceDirectory,
                    TargetPath = job.TargetDirectory,
                    Type = job.Type,
                    State = BackupState.Active,
                    LastActionTimestamp = DateTime.Now
                };

                // Ajouter l'état à la liste AVANT l'exécution pour que le fichier state.json soit à jour pendant la copie
                states.Add(jobState);
                _stateRepository.UpdateState(states);
                OnProgressChanged?.Invoke(jobState);

                try
                {
                    // Créer la stratégie appropriée selon le type de sauvegarde
                    BackupStrategy strategy = CreateBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Type, job.Name);

                    // S'abonner à l'événement d'initialisation (nombre total de fichiers + taille)
                    strategy.OnBackupInitialized += (totalFiles, totalSize) =>
                    {
                        jobState.TotalFiles = totalFiles;
                        jobState.TotalSize = totalSize;
                        jobState.RemainingFiles = totalFiles;
                        jobState.RemainingSize = totalSize;
                        jobState.LastActionTimestamp = DateTime.Now;
                        _stateRepository.UpdateState(states);
                        OnProgressChanged?.Invoke(jobState);
                    };

                    // S'abonner à l'événement de transfert de fichier (progression fichier par fichier)
                    strategy.OnFileTransferred += (sourceFile, targetFile, fileSize) =>
                    {
                        jobState.RemainingFiles--;
                        jobState.RemainingSize -= fileSize;
                        jobState.CurrentSourceFile = sourceFile;
                        jobState.CurrentTargetFile = targetFile;
                        jobState.LastActionTimestamp = DateTime.Now;
                        _stateRepository.UpdateState(states);
                        OnProgressChanged?.Invoke(jobState);
                    };

                    // Exécuter la sauvegarde
                    var (success, errorMessage) = strategy.Execute();

                    // Mettre à jour l'état final
                    jobState.State = success ? BackupState.Completed : BackupState.Error;
                    jobState.CurrentSourceFile = string.Empty;
                    jobState.CurrentTargetFile = string.Empty;
                    jobState.RemainingFiles = 0;
                    jobState.RemainingSize = 0;
                    jobState.LastActionTimestamp = DateTime.Now;
                    _stateRepository.UpdateState(states);
                    OnProgressChanged?.Invoke(jobState);

                    if (success)
                    {
                        results.Add($"Sauvegarde '{job.Name}' terminée avec succès.");
                    }
                    else
                    {
                        results.Add($"Erreur lors de la sauvegarde '{job.Name}' : {errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    jobState.State = BackupState.Error;
                    jobState.LastActionTimestamp = DateTime.Now;
                    _stateRepository.UpdateState(states);
                    OnProgressChanged?.Invoke(jobState);
                    results.Add($"Exception lors de la sauvegarde '{job.Name}' : {ex.Message}");
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