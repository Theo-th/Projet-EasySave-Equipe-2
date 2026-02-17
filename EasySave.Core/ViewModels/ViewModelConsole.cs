using EasySave.Core.Services;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;

namespace EasySave.Core.ViewModels
{
    public class ViewModelConsole
    {
        private readonly IJobConfigService _configService;
        private readonly BackupService _backupService; // Changé de IBackupService à BackupService pour accéder à SetLogTarget
        private readonly IBackupStateRepository _backupState;
        private readonly ProcessDetector _processDetector;
        private LogType _currentLogType;

        public event Action<BackupJobState>? OnProgressChanged;
        public event Action<string>? OnBusinessProcessDetected;

        public ViewModelConsole(LogType logType = LogType.JSON, string? configPath = null, string? statePath = null, string? logsPath = null)
        {
            _configService = new JobConfigService(configPath ?? "jobs_config.json");
            _backupState = new BackupStateRepository();
            if (!string.IsNullOrEmpty(statePath)) _backupState.SetStatePath(statePath);
            _currentLogType = logType;

            _processDetector = new ProcessDetector();
            _processDetector.StartContinuousMonitoring();

            _backupService = new BackupService(_configService, _backupState, _processDetector, logType, logsPath);

            _backupService.OnProgressChanged += (state) => OnProgressChanged?.Invoke(state);
            _backupService.OnBusinessProcessDetected += (processName) => OnBusinessProcessDetected?.Invoke(processName);
        }

        public void SetLogTarget(string targetName)
        {
            if (Enum.TryParse<LogTarget>(targetName, out var target))
            {
                _backupService.SetLogTarget(target);
            }
        }

        public (bool Success, string? ErrorMessage) CreateJob(string? name, string? source, string? destination, BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name)) return (false, "Job name is required.");
            if (string.IsNullOrWhiteSpace(source)) return (false, "Source directory is required.");
            if (string.IsNullOrWhiteSpace(destination)) return (false, "Destination directory is required.");
            return _configService.CreateJob(name.Trim(), source.Trim(), destination.Trim(), type);
        }

        public string? ExecuteJobs(List<int> jobIndices) => _backupService.ExecuteBackup(jobIndices);
        public void PauseBackup() => _backupService.PauseBackup();
        public void ResumeBackup() => _backupService.ResumeBackup();
        public void StopBackup() => _backupService.StopBackup();
        public bool DeleteJob(int jobIndex) => _configService.RemoveJob(jobIndex);

        public List<string> GetAllJobs() => _configService.GetAllJobs().ConvertAll(job => job.Name);

        public string? GetJob(int jobIndex)
        {
            var job = _configService.GetJob(jobIndex);
            return job != null ? $"{job.Name} -- {job.Type} -- {job.SourceDirectory} -- {job.TargetDirectory}" : null;
        }

        public string CurrentLogFormat() => _currentLogType.ToString();

        public void ChangeLogFormat(string format)
        {
            if (Enum.TryParse<LogType>(format, ignoreCase: true, out var logType))
            {
                _currentLogType = logType;
                _backupService.ChangeLogFormat(logType);
            }
        }

        public void UpdateLogsPath(string logsPath) => _backupService.UpdateLogsDirectory(logsPath);
        public void UpdateConfigPath(string configPath) => _configService.UpdateConfigPath(configPath);
        public void UpdateStatePath(string statePath) => _backupState.SetStatePath(statePath);

        public string GetEncryptionKey() => EncryptionService.Instance.GetKey();
        public void SetEncryptionKey(string key) => EncryptionService.Instance.SetKey(key);
        public List<string> GetEncryptionExtensions() => EncryptionService.Instance.GetExtensions();
        public void AddEncryptionExtension(string extension) => EncryptionService.Instance.AddExtension(extension);
        public void RemoveEncryptionExtension(string extension) => EncryptionService.Instance.RemoveExtension(extension);

        public void AddWatchedProcess(string processName) => _processDetector.AddWatchedProcess(processName);
        public void RemoveWatchedProcess(string processName) => _processDetector.RemoveWatchedProcess(processName);
        public List<string> GetWatchedProcesses() => _processDetector.GetWatchedProcesses();
    }
}