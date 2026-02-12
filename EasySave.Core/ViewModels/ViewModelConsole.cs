using EasySave.Core.Services;
using Projet_EasySave.EasyLog;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

namespace EasySave.Core.ViewModels
{
    /// <summary>
    /// Manages console interactions and coordinates communication between backup jobs and the console view.
    /// </summary>
    public class ViewModelConsole
    {
        private readonly IJobConfigService _configService;
        private readonly IBackupService _backupService;

        public ViewModelConsole(IJobConfigService? configService = null, IBackupStateRepository? stateRepository = null, string? customLogPath = null, string? customConfigPath = null, string? customStatePath = null)
        {
            string configPath = customConfigPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jobs_config.json");
            _configService = configService ?? new JobConfigService(configPath);
            
            string statePath = customStatePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");
            var repo = stateRepository ?? new BackupStateRepository();
            repo.SetStatePath(statePath);

            // Créer le fichier state.json avec un état vide au démarrage si nécessaire
            repo.UpdateState(new List<BackupJobState>());

            string logPath = customLogPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            JsonLog myLogger = new JsonLog(logPath);

            _backupService = new BackupService(_configService, myLogger, repo);
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
        /// Executes a backup job by its index.
        /// </summary>
        public string? ExecuteJob(int jobIndex)
        {
            return _backupService.ExecuteBackup(jobIndex);
        }

        /// <summary>
        /// Executes multiple backup jobs.
        /// </summary>
        public List<(int Index, string? Message)> ExecuteJobs(List<int> jobIndices)
        {
            var results = new List<(int, string?)>();
            foreach (int index in jobIndices)
            {
                string? message = _backupService.ExecuteBackup(index);
                results.Add((index, message));
            }
            return results;
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
            var job = _configService.LoadJob(jobIndex);
            return job != null ? $"{job.Name} -- {job.Type}" : null;
        }

        /// <summary>
        /// Gets full job details by index.
        /// </summary>
        public BackupJob? GetJobDetails(int jobIndex)
        {
            return _configService.LoadJob(jobIndex);
        }
    }
}
