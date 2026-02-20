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
        private readonly BackupService _backupService;
        private readonly IBackupStateRepository _backupState;
        private readonly ProcessDetector _processDetector;
        private LogType _currentLogType;

        // Event triggered on each backup job progress change.
        // The view can subscribe to it to display a loading bar.
        public event Action<BackupJobState>? OnProgressChanged;

        // Event triggered when a watched business process is detected during backup.
        public event Action<string>? OnBusinessProcessDetected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelConsole"/> class.
        /// </summary>
        /// <param name="logType">The log format type.</param>
        /// <param name="configPath">The path to the config file.</param>
        /// <param name="statePath">The path to the state file.</param>
        /// <param name="logsPath">The path to the logs directory.</param>
        /// <param name="maxSimultaneousJobs">Maximum number of simultaneous jobs (default: 3).</param>
        /// <param name="fileSizeThresholdMB">File size threshold in MB (default: 10).</param>
        /// <param name="priorityExtensions">List of priority file extensions.</param>
        public ViewModelConsole(LogType logType = LogType.JSON, string? configPath = null, string? statePath = null, string? logsPath = null, int maxSimultaneousJobs = 3, int fileSizeThresholdMB = 10, List<string>? priorityExtensions = null)
        {
            _configService = new JobConfigService(configPath ?? "jobs_config.json");
            _backupState = new BackupStateRepository();
            if (!string.IsNullOrEmpty(statePath))
            {
                _backupState.SetStatePath(statePath);
            }
            _currentLogType = logType;

            // Create the shared ProcessDetector instance
            _processDetector = new ProcessDetector();
            _processDetector.StartContinuousMonitoring();

            _backupService = new BackupService(_configService, _backupState, _processDetector, logType, logsPath, maxSimultaneousJobs, fileSizeThresholdMB, priorityExtensions);

            // Relay the event from the service to the view
            _backupService.OnProgressChanged += (state) => OnProgressChanged?.Invoke(state);
            _backupService.OnBusinessProcessDetected += (processName) => OnBusinessProcessDetected?.Invoke(processName);
        }

        /// <summary>
        /// Creates a new backup job.
        /// </summary>
        /// <param name="name">The job name.</param>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The destination directory.</param>
        /// <param name="type">The backup type.</param>
        /// <returns>Tuple indicating success and an optional error message.</returns>
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


        public void SetLogTarget(string targetName)
        {
            if (Enum.TryParse<LogTarget>(targetName, out var target))
            {
                _backupService.SetLogTarget(target);
            }
        }



        /// <summary>
        /// Executes multiple backup jobs.
        /// </summary>
        /// <param name="jobIndices">The indices of jobs to execute.</param>
        /// <returns>Execution result message.</returns>
        public string? ExecuteJobs(List<int> jobIndices)
        {
            string? message = _backupService.ExecuteBackup(jobIndices);

            return message;
        }

        /// <summary>
        /// Pauses the currently active backup.
        /// </summary>
        public void PauseBackup()
        {
            _backupService.PauseBackup();
        }

        /// <summary>
        /// Resumes the currently paused backup.
        /// </summary>
        public void ResumeBackup()
        {
            _backupService.ResumeBackup();
        }

        /// <summary>
        /// Stops (cancels) the currently active backup.
        /// </summary>
        public void StopBackup()
        {
            _backupService.StopBackup();
        }

        /// <summary>
        /// Deletes a backup job by its index.
        /// </summary>
        /// <param name="jobIndex">The index of the job to delete.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        public bool DeleteJob(int jobIndex)
        {
            return _configService.RemoveJob(jobIndex);
        }

        /// <summary>
        /// Gets all configured backup job names.
        /// </summary>
        /// <returns>List of job names.</returns>
        public List<string> GetAllJobs()
        {
            var jobs = _configService.GetAllJobs();
            return jobs.ConvertAll(job => job.Name);
        }

        /// <summary>
        /// Gets job name and type by index.
        /// </summary>
        /// <param name="jobIndex">The index of the job.</param>
        /// <returns>Job info string or null.</returns>
        public string? GetJob(int jobIndex)
        {
            var job = _configService.GetJob(jobIndex);
            return job != null ? $"{job.Name} -- {job.Type} -- {job.SourceDirectory} -- {job.TargetDirectory}" : null;
        }

        /// <summary>
        /// Gets the current log format as a string.
        /// </summary>
        /// <returns>The log format.</returns>
        public string CurrentLogFormat()
        {
            return _currentLogType.ToString();
        }

        /// <summary>
        /// Changes the log format.
        /// </summary>
        /// <param name="format">The new log format.</param>
        public void ChangeLogFormat(string format)
        {
            if (Enum.TryParse<LogType>(format, ignoreCase: true, out var logType))
            {
                _currentLogType = logType;
                _backupService.ChangeLogFormat(logType);
            }
        }

        /// <summary>
        /// Updates multi-threading parameters.
        /// </summary>
        /// <param name="maxSimultaneousJobs">Maximum number of simultaneous jobs.</param>
        /// <param name="fileSizeThresholdMB">File size threshold in MB.</param>
        public void UpdateThreadingSettings(int maxSimultaneousJobs, int fileSizeThresholdMB)
        {
            _backupService.UpdateThreadingSettings(maxSimultaneousJobs, fileSizeThresholdMB);
        }

        /// <summary>
        /// Updates priority extensions list.
        /// </summary>
        /// <param name="extensions">List of priority extensions.</param>
        public void UpdatePriorityExtensions(List<string> extensions)
        {
            _backupService.UpdatePriorityExtensions(extensions);
        }

        /// <summary>
        /// Gets the current list of priority extensions.
        /// </summary>
        public List<string> GetPriorityExtensions()
        {
            return _backupService.GetPriorityExtensions();
        }

        /// <summary>
        /// Updates the logs directory path without recreating the entire ViewModel.
        /// </summary>
        /// <param name="logsPath">The new logs directory path.</param>
        public void UpdateLogsPath(string logsPath)
        {
            _backupService.UpdateLogsDirectory(logsPath);
        }

        /// <summary>
        /// Updates the config file path without recreating the entire ViewModel.
        /// </summary>
        /// <param name="configPath">The new config file path.</param>
        public void UpdateConfigPath(string configPath)
        {
            _configService.UpdateConfigPath(configPath);
        }

        /// <summary>
        /// Updates the state file path without recreating the entire ViewModel.
        /// </summary>
        /// <param name="statePath">The new state file path.</param>
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

        // ==================== Process Detector ====================

        /// <summary>
        /// Adds a business process to the watch list.
        /// </summary>
        public void AddWatchedProcess(string processName)
        {
            _processDetector.AddWatchedProcess(processName);
        }

        /// <summary>
        /// Removes a business process from the watch list.
        /// </summary>
        public void RemoveWatchedProcess(string processName)
        {
            _processDetector.RemoveWatchedProcess(processName);
        }

        /// <summary>
        /// Gets the list of currently watched business processes.
        /// </summary>
        public List<string> GetWatchedProcesses()
        {
            return _processDetector.GetWatchedProcesses();
        }


        /// <summary>
        /// Gets the ip of currently server processes.
        /// </summary>
        public string GetServerIp() => NetworkService.Instance.GetServerIp();

        /// <summary>
        /// Sets the ip of server processes.
        /// </summary>
        public void SetServerIp(string ip) => NetworkService.Instance.SetServerIp(ip);
    }
}