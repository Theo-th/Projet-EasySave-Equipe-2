using EasyLog;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;
using EasySave.Core.Services.Strategies;

namespace EasySave.Core.Services
{
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly IBackupStateRepository _stateRepository;
        private readonly ProcessDetector _processDetector;
        private BaseLog _logger;
        private string _logDirectory;
        private BackupStrategy? _activeStrategy;
        private LogTarget _currentLogTarget = LogTarget.Both;

        public event Action<BackupJobState>? OnProgressChanged;
        public event Action<string>? OnBusinessProcessDetected;

        public BackupService(IJobConfigService configService, IBackupStateRepository stateRepository, ProcessDetector processDetector, LogType logType, string? logDirectory = null)
        {
            _configService = configService;
            _stateRepository = stateRepository;
            _processDetector = processDetector;
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            ChangeLogFormat(logType);
            _processDetector.ProcessStatusChanged += OnWatchedProcessStatusChanged;
        }

        // Méthode pour définir la cible
        public void SetLogTarget(LogTarget target)
        {
            _currentLogTarget = target;
        }

        private void OnWatchedProcessStatusChanged(object? sender, ProcessStatusChangedEventArgs e)
        {
            if (e.IsRunning && _activeStrategy != null)
            {
                _activeStrategy.Cancel();
                OnBusinessProcessDetected?.Invoke(e.Process.ProcessName);
            }
        }

        public void PauseBackup() => _activeStrategy?.Pause();
        public void ResumeBackup() => _activeStrategy?.Resume();
        public void StopBackup() => _activeStrategy?.Cancel();

        public string? ExecuteBackup(List<int> jobIndices)
        {
            if (jobIndices == null || jobIndices.Count == 0) return "No backup job specified.";

            var runningProcess = _processDetector.IsAnyWatchedProcessRunning();
            if (runningProcess != null)
            {
                OnBusinessProcessDetected?.Invoke(runningProcess);
                return $"Backup aborted: business process '{runningProcess}' is currently running.";
            }

            var allJobs = _configService.GetAllJobs();
            var results = new List<string>();
            var states = new List<BackupJobState>();

            foreach (int index in jobIndices)
            {
                if (index < 0 || index >= allJobs.Count) continue;

                runningProcess = _processDetector.IsAnyWatchedProcessRunning();
                if (runningProcess != null)
                {
                    OnBusinessProcessDetected?.Invoke(runningProcess);
                    results.Add($"Backup aborted: business process '{runningProcess}' detected.");
                    break;
                }

                BackupJob job = allJobs[index];
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

                states.Add(jobState);
                _stateRepository.UpdateState(states);
                OnProgressChanged?.Invoke(jobState);

                try
                {
                    BackupStrategy strategy = CreateBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Type, job.Name);
                    _activeStrategy = strategy;

                    strategy.OnPauseStateChanged += (isPaused) =>
                    {
                        jobState.State = isPaused ? BackupState.Paused : BackupState.Active;
                        jobState.LastActionTimestamp = DateTime.Now;
                        _stateRepository.UpdateState(states);
                        OnProgressChanged?.Invoke(jobState);
                    };

                    strategy.OnBackupInitialized += (totalFiles, totalSize) =>
                    {
                        jobState.TotalFiles = totalFiles; jobState.TotalSize = totalSize;
                        jobState.RemainingFiles = totalFiles; jobState.RemainingSize = totalSize;
                        jobState.StartTimestamp = DateTime.Now; jobState.LastActionTimestamp = DateTime.Now;
                        _stateRepository.UpdateState(states); OnProgressChanged?.Invoke(jobState);
                    };

                    strategy.OnFileTransferred += (sourceFile, targetFile, fileSize) =>
                    {
                        jobState.RemainingFiles--; jobState.RemainingSize -= fileSize;
                        jobState.CurrentSourceFile = sourceFile; jobState.CurrentTargetFile = targetFile;
                        jobState.LastActionTimestamp = DateTime.Now;
                        _stateRepository.UpdateState(states); OnProgressChanged?.Invoke(jobState);
                    };

                    var (success, errorMessage) = strategy.Execute();

                    jobState.State = success ? BackupState.Completed : BackupState.Error;
                    jobState.CurrentSourceFile = ""; jobState.CurrentTargetFile = "";
                    jobState.RemainingFiles = 0; jobState.RemainingSize = 0;
                    jobState.LastActionTimestamp = DateTime.Now;
                    _stateRepository.UpdateState(states); OnProgressChanged?.Invoke(jobState);

                    results.Add(success ? $"Backup '{job.Name}' completed successfully." : $"Error during backup '{job.Name}': {errorMessage}");
                }
                catch (Exception ex)
                {
                    jobState.State = BackupState.Error;
                    _stateRepository.UpdateState(states);
                    OnProgressChanged?.Invoke(jobState);
                    results.Add($"Exception during backup '{job.Name}': {ex.Message}");
                }
                finally { _activeStrategy = null; }
            }
            return string.Join("\n", results);
        }

        private BackupStrategy CreateBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName)
        {
            return backupType switch
            {
                BackupType.Complete => new FullBackupStrategy(sourceDirectory, targetDirectory, backupType, jobName, _logger, _currentLogTarget),
                BackupType.Differential => new DifferentialBackupStrategy(sourceDirectory, targetDirectory, backupType, jobName, _logger, _currentLogTarget),
                _ => throw new InvalidOperationException($"Unsupported backup type: {backupType}")
            };
        }

        public void ChangeLogFormat(LogType logType)
        {
            _logger = logType switch { LogType.JSON => new JsonLog(_logDirectory), LogType.XML => new XmlLog(_logDirectory), _ => new JsonLog(_logDirectory) };
        }

        public void UpdateLogsDirectory(string newLogsDirectory)
        {
            _logDirectory = newLogsDirectory;
            if (_logger is JsonLog) _logger = new JsonLog(_logDirectory);
            else if (_logger is XmlLog) _logger = new XmlLog(_logDirectory);
        }
    }
}