using Projet_EasySave.Services;
using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;

namespace Projet_EasySave.ViewModels
{
    /// <summary>
    /// Manages console interactions and coordinates communication between backup jobs and the console view.
    /// </summary>
    public class ViewModelConsole
    {
        private readonly IJobConfigService _configService;
        private readonly IBackupService _backupService;
        private readonly IBackupStateRepository _backupState;

        /// <summary>
        /// Événement déclenché à chaque changement de progression d'un travail de sauvegarde.
        /// La vue peut s'y abonner pour afficher une barre de chargement.
        /// </summary>
        public event Action<BackupJobState>? OnProgressChanged;

        public ViewModelConsole(LogType logType = LogType.JSON)
        {
            _configService = new JobConfigService();
            _backupState = new BackupStateRepository();
            _backupService = new BackupService(_configService, _backupState, logType);

            // Relayer l'événement du service vers la vue
            _backupService.OnProgressChanged += (state) => OnProgressChanged?.Invoke(state);
        }

        /// <summary>
        /// Creates a new backup job.
        /// </summary>
        /// <returns>Tuple indicating success and an optional error message</returns>
        public (bool Success, string? ErrorMessage) CreateJob(string? name, string? source, string? destination, BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Le nom du travail est requis.");

            if (string.IsNullOrWhiteSpace(source))
                return (false, "Le répertoire source est requis.");

            if (string.IsNullOrWhiteSpace(destination))
                return (false, "Le répertoire de destination est requis.");

            return _configService.CreateJob(name.Trim(), source.Trim(), destination.Trim(), type);
        }

        /// <summary>
        /// Executes multiple backup jobs.
        /// </summary>
        public string? ExecuteJobs(List<int> jobIndices)
        {
            string? message = _backupService.ExecuteBackup(jobIndices);
            
            return message;
        }

        /// <summary>
        /// Deletes a backup job by its index.
        /// </summary>
        public bool DeleteJob(int jobIndex)
        {
            return _configService.RemoveJob(jobIndex);
        }

        /// <summary>
        /// Gets all configured backup job names.
        /// </summary>
        public List<string> GetAllJobs()
        {
            var jobs = _configService.GetAllJobs();
            return jobs.ConvertAll(job => job.Name);
        }

        /// <summary>
        /// Gets job name and type by index.
        /// </summary>
        public string? GetJob(int jobIndex)
        {
            var job = _configService.GetJob(jobIndex);
            return job != null ? $"{job.Name} -- {job.Type}" : null;
        }
    }
}
