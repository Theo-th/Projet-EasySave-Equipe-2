using EasySave.Core.Services;
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
        private readonly IBackupStateRepository _backupState;
        private LogType _currentLogType;

        /// <summary>
        /// Event triggered on each backup job progress change.
        /// The view can subscribe to it to display a loading bar.
        /// </summary>
        public event Action<BackupJobState>? OnProgressChanged;

        public ViewModelConsole(LogType logType = LogType.JSON, string? configPath = null, string? statePath = null, string? logsPath = null)
        {
            _configService = new JobConfigService(configPath ?? "jobs_config.json");
            _backupState = new BackupStateRepository();
            if (!string.IsNullOrEmpty(statePath))
            {
                _backupState.SetStatePath(statePath);
            }
            _currentLogType = logType;
            _backupService = new BackupService(_configService, _backupState, logType, logsPath);

            // Relay the event from the service to the view
            _backupService.OnProgressChanged += (state) => OnProgressChanged?.Invoke(state);
        }

        /// <summary>
        /// Creates a new backup job.
        /// </summary>
        /// <returns>Tuple indicating success and an optional error message</returns>
        public (bool Success, string? ErrorMessage) CreateJob(string? name, string? source, string? destination, BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Job name is required.");

            if (string.IsNullOrWhiteSpace(source))
                return (false, "Source directory is required.");

            if (string.IsNullOrWhiteSpace(destination))
                return (false, "Destination directory is required.");

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

        public string CurrentLogFormat() 
        {
            return _currentLogType.ToString();
        }

        public void ChangeLogFormat(string format)
        {
            if (Enum.TryParse<LogType>(format, ignoreCase: true, out var logType))
            {
                _currentLogType = logType;
                _backupService.ChangeLogFormat(logType);
            }
        }

        /// <summary>
        /// Updates the logs directory path without recreating the entire ViewModel.
        /// </summary>
        public void UpdateLogsPath(string logsPath)
        {
            _backupService.UpdateLogsDirectory(logsPath);
        }

        /// <summary>
        /// Updates the config file path without recreating the entire ViewModel.
        /// </summary>
        public void UpdateConfigPath(string configPath)
        {
            _configService.UpdateConfigPath(configPath);
        }

        /// <summary>
        /// Updates the state file path without recreating the entire ViewModel.
        /// </summary>
        public void UpdateStatePath(string statePath)
        {
            _backupState.SetStatePath(statePath);
        }

        /// <summary>
        /// Retrieves the current encryption key.
        /// </summary>
        public string GetEncryptionKey()
        {
            return EncryptionService.Instance.GetKey();
        }

        /// <summary>
        /// Updates the encryption key.
        /// </summary>
        /// <param name="key">The new key to set.</param>
        public void SetEncryptionKey(string key)
        {
            EncryptionService.Instance.SetKey(key);
        }

        /// <summary>
        /// Retrieves the list of file extensions configured for encryption.
        /// </summary>
        /// <returns>A list of extensions (e.g., ".txt", ".json").</returns>
        public List<string> GetEncryptionExtensions()
        {
            return EncryptionService.Instance.GetExtensions();
        }

        /// <summary>
        /// Adds a file extension to the encryption list.
        /// </summary>
        /// <param name="extension">The extension to add (e.g., ".txt").</param>
        public void AddEncryptionExtension(string extension)
        {
            EncryptionService.Instance.AddExtension(extension);
        }

        /// <summary>
        /// Removes a file extension from the encryption list.
        /// </summary>
        /// <param name="extension">The extension to remove.</param>
        public void RemoveEncryptionExtension(string extension)
        {
            EncryptionService.Instance.RemoveExtension(extension);
        }
    }
}
