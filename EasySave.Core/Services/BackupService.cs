using EasyLog;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;
using EasySave.Core.Services.Strategies;


namespace EasySave.Core.Services
{
    /// <summary>
    /// Service for executing and managing backup operations.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly IBackupStateRepository _stateRepository;
        private BaseLog _logger;
        private string _logDirectory;

        /// <summary>
        /// Event triggered on each backup job progress change.
        /// </summary>
        public event Action<BackupJobState>? OnProgressChanged;

        public BackupService(IJobConfigService configService, IBackupStateRepository stateRepository, LogType logType, string? logDirectory = null)
        {
            _configService = configService;
            _stateRepository = stateRepository;

            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            _logger = logType switch
            {
                LogType.JSON => new JsonLog(_logDirectory),
                LogType.XML => new XmlLog(_logDirectory),
                _ => new JsonLog(_logDirectory)
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
                return "No backup job specified.";
            }

            var allJobs = _configService.GetAllJobs();
            var results = new List<string>();
            var states = new List<BackupJobState>();

            foreach (int index in jobIndices)
            {
                if (index < 0 || index >= allJobs.Count)
                {
                    results.Add($"Error: Index {index} is invalid.");
                    continue;
                }

                BackupJob job = allJobs[index];

                // Initialize the job state
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

                // Add the state to the list BEFORE execution so the state.json file is up-to-date during copy
                states.Add(jobState);
                _stateRepository.UpdateState(states);
                OnProgressChanged?.Invoke(jobState);

                try
                {
                    // Create the appropriate strategy based on the backup type
                    BackupStrategy strategy = CreateBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Type, job.Name);

                    // Subscribe to the initialization event (total file count + size)
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

                    // Subscribe to the file transfer event (file-by-file progress)
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

                    // Execute the backup
                    var (success, errorMessage) = strategy.Execute();

                    // Update the final state
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
                        results.Add($"Backup '{job.Name}' completed successfully.");
                    }
                    else
                    {
                        results.Add($"Error during backup '{job.Name}': {errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    jobState.State = BackupState.Error;
                    jobState.LastActionTimestamp = DateTime.Now;
                    _stateRepository.UpdateState(states);
                    OnProgressChanged?.Invoke(jobState);
                    results.Add($"Exception during backup '{job.Name}': {ex.Message}");
                }
            }

            return string.Join("\n", results);
        }

        /// <summary>
        /// Creates the appropriate backup strategy based on the type.
        /// </summary>
        private BackupStrategy CreateBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName)
        {
            return backupType switch
            {
                BackupType.Complete => new FullBackupStrategy(sourceDirectory, targetDirectory, backupType, jobName, _logger),
                BackupType.Differential => new DifferentialBackupStrategy(sourceDirectory, targetDirectory, backupType, jobName, _logger),
                _ => throw new InvalidOperationException($"Unsupported backup type: {backupType}")
            };
        }

        public void ChangeLogFormat(LogType logType)
        {
            _logger = logType switch
            {
                LogType.JSON => new JsonLog(_logDirectory),
                LogType.XML => new XmlLog(_logDirectory),
                _ => new JsonLog(_logDirectory)
            };
        }
    }
}