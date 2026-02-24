using EasyLog;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;
using EasySave.Core.Services.Strategies;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Main multi-threaded backup service.
    /// Orchestrates 3 phases: Analysis → Categorization → Execution.
    /// Supports global AND per-job control (pause/resume/stop).
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly BackupLogManager _logManager;
        private readonly BusinessProcessManager _processManager;
        private readonly JobStateTracker _stateTracker;
        private readonly JobControlCoordinator _controlCoordinator;

        // Multi-threading configuration
        private int _maxSimultaneousJobs;
        private long _sizeThreshold;
        private HashSet<string> _priorityExtensions;
        private readonly object _configLock = new();

        // Global mutex: prevents two heavy file transfers simultaneously
        private readonly SemaphoreSlim _heavyFileSemaphore = new SemaphoreSlim(1, 1);

        // Events
        public event Action<BackupJobState>? OnProgressChanged;
        public event Action<string>? OnBusinessProcessDetected;

        /// <summary>
        /// Initializes a new instance of <see cref="BackupService"/> with the specified configuration and threading settings.
        /// </summary>
        public BackupService(
            IJobConfigService configService,
            IBackupStateRepository stateRepository,
            ProcessDetector processDetector,
            LogType logType,
            string? logDirectory = null,
            int maxSimultaneousJobs = 3,
            int fileSizeThresholdMB = 10,
            List<string>? priorityExtensions = null)
        {
            _configService = configService;
            _stateTracker = new JobStateTracker(stateRepository);
            _controlCoordinator = new JobControlCoordinator();
            _logManager = new BackupLogManager(logType, logDirectory);
            _processManager = new BusinessProcessManager(processDetector,
                (from, to) => _stateTracker.UpdateAllJobStates(from, to), null);

            _maxSimultaneousJobs = Math.Clamp(maxSimultaneousJobs, 1, 10);
            _sizeThreshold = (long)fileSizeThresholdMB * 1_000_000;

            _priorityExtensions = priorityExtensions != null
                ? new HashSet<string>(priorityExtensions, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            _stateTracker.OnStateChanged += (state) => OnProgressChanged?.Invoke(state);
            _processManager.OnBusinessProcessDetected += (processName) => OnBusinessProcessDetected?.Invoke(processName);
        }

        // ================================================================
        //  CONFIGURATION
        // ================================================================

        /// <summary>
        /// Sets the log output target (file, console, etc.).
        /// </summary>
        public void SetLogTarget(LogTarget target) => _logManager.SetLogTarget(target);

        /// <summary>
        /// Changes the log serialization format (JSON, XML, etc.).
        /// </summary>
        public void ChangeLogFormat(LogType logType) => _logManager.ChangeLogFormat(logType);

        /// <summary>
        /// Updates the directory where log files are written.
        /// </summary>
        public void UpdateLogsDirectory(string newLogsDirectory) => _logManager.UpdateLogsDirectory(newLogsDirectory);

        /// <summary>
        /// Updates the maximum number of simultaneous jobs and the heavy-file size threshold.
        /// </summary>
        public void UpdateThreadingSettings(int maxSimultaneousJobs, int fileSizeThresholdMB)
        {
            lock (_configLock)
            {
                _maxSimultaneousJobs = Math.Clamp(maxSimultaneousJobs, 1, 10);
                _sizeThreshold = (long)fileSizeThresholdMB * 1_000_000;
            }
        }

        /// <summary>
        /// Replaces the set of file extensions that are treated as priority during backup.
        /// </summary>
        public void UpdatePriorityExtensions(List<string> extensions)
        {
            lock (_configLock)
            {
                _priorityExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Returns the current list of priority file extensions.
        /// </summary>
        public List<string> GetPriorityExtensions()
        {
            lock (_configLock) { return _priorityExtensions.ToList(); }
        }

        // ================================================================
        //  GLOBAL CONTROL (ALL JOBS)
        // ================================================================

        /// <summary>
        /// Pauses all currently running backup jobs.
        /// </summary>
        public void PauseBackup()
        {
            _controlCoordinator.PauseAllJobs();
            _stateTracker.UpdateAllJobStates(BackupState.Active, BackupState.Paused);
        }

        /// <summary>
        /// Resumes all paused backup jobs.
        /// </summary>
        public void ResumeBackup()
        {
            _controlCoordinator.ResumeAllJobs();
            _stateTracker.UpdateAllJobStates(BackupState.Paused, BackupState.Active);
        }

        /// <summary>
        /// Stops all active and paused backup jobs.
        /// </summary>
        public void StopBackup()
        {
            _controlCoordinator.StopAllJobs();
            _stateTracker.UpdateAllJobStates(BackupState.Active, BackupState.Inactive);
            _stateTracker.UpdateAllJobStates(BackupState.Paused, BackupState.Inactive);
        }

        // ================================================================
        //  PER-JOB CONTROL
        // ================================================================

        /// <summary>
        /// Pauses the backup job identified by <paramref name="jobName"/>.
        /// </summary>
        public void PauseJob(string jobName)
        {
            _controlCoordinator.PauseJob(jobName);
            _stateTracker.UpdateJobState(jobName, state => state.State = BackupState.Paused);
        }

        /// <summary>
        /// Resumes the backup job identified by <paramref name="jobName"/>.
        /// </summary>
        public void ResumeJob(string jobName)
        {
            _controlCoordinator.ResumeJob(jobName);
            _stateTracker.UpdateJobState(jobName, state => state.State = BackupState.Active);
        }

        /// <summary>
        /// Stops the backup job identified by <paramref name="jobName"/>.
        /// </summary>
        public void StopJob(string jobName)
        {
            _controlCoordinator.StopJob(jobName);
            _stateTracker.UpdateJobState(jobName, state => state.State = BackupState.Inactive);
        }

        // ================================================================
        //  MAIN EXECUTION
        // ================================================================

        /// <summary>
        /// Executes the backup jobs corresponding to the given list of indices.
        /// Runs in three phases: analysis, global-queue population, then parallel copy.
        /// Returns an error message string if any errors occurred, or <c>null</c> on full success.
        /// </summary>
        public string? ExecuteBackup(List<int> jobIndices)
        {
            if (jobIndices == null || jobIndices.Count == 0)
                return "No backup job specified.";

            // Full reset
            _controlCoordinator.StopAllJobs();
            _controlCoordinator.ResumeAllJobs();
            _stateTracker.ClearStates();

            var allJobs = _configService.GetAllJobs();
            var errors = new ConcurrentBag<string>();

            // Collect valid jobs
            var validJobs = new List<(BackupJob Job, int Index)>();
            foreach (var jobIndex in jobIndices)
            {
                if (jobIndex < 0 || jobIndex >= allJobs.Count)
                {
                    errors.Add($"Invalid job index: {jobIndex}");
                    continue;
                }
                validJobs.Add((allJobs[jobIndex], jobIndex));
            }

            if (validJobs.Count == 0)
                return errors.Count > 0 ? string.Join("\n", errors) : "No backup job specified.";

            // PRE-REGISTRATION: all jobs set to Inactive (waiting)
            foreach (var (job, index) in validJobs)
            {
                _controlCoordinator.RegisterJob(job.Name);
                _stateTracker.RegisterJob(job.Name, new BackupJobState
                {
                    Id = index + 1,
                    Name = job.Name,
                    SourcePath = job.SourceDirectory,
                    TargetPath = job.TargetDirectory,
                    Type = job.Type,
                    State = BackupState.Inactive
                });
            }

            // ── PHASE 1: PARALLEL ANALYSIS ──────────────────────────────────
            // Each job analyses its files and enqueues them into the global queue.
            var globalQueue = new GlobalFileQueue();
            var jobsWithFiles = new ConcurrentDictionary<string, bool>();

            HashSet<string> priorityExtCopy;
            lock (_configLock) { priorityExtCopy = new HashSet<string>(_priorityExtensions, StringComparer.OrdinalIgnoreCase); }

            var analyseTasks = validJobs.Select(entry => Task.Run(() =>
            {
                var (job, idx) = entry;
                try
                {
                    _stateTracker.UpdateJobState(job.Name, s => s.State = BackupState.Active);

                    BackupStrategy strategy = job.Type == BackupType.Complete
                        ? new FullBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, priorityExtCopy)
                        : new DifferentialBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, priorityExtCopy);

                    var files = strategy.Analyze();
                    long totalSize = files.Sum(f => f.FileSize);

                    _stateTracker.UpdateJobState(job.Name, s =>
                    {
                        s.TotalFiles = files.Count;
                        s.TotalSize = totalSize;
                        s.RemainingFiles = files.Count;
                        s.RemainingSize = totalSize;
                    });

                    if (files.Count == 0)
                    {
                        _stateTracker.FinalizeJobState(job.Name);
                        _controlCoordinator.UnregisterJob(job.Name);
                        return;
                    }

                    strategy.Prepare();
                    jobsWithFiles[job.Name] = true;

                    // Register as producer before enqueuing files
                    globalQueue.RegisterProducer();
                    foreach (var file in files)
                        globalQueue.Enqueue(file);
                    globalQueue.ProducerDone();
                }
                catch (Exception ex)
                {
                    errors.Add($"Error analysing '{job.Name}': {ex.Message}");
                    _stateTracker.UpdateJobState(job.Name, s => s.State = BackupState.Error);
                    _controlCoordinator.UnregisterJob(job.Name);
                }
            })).ToArray();

            Task.WaitAll(analyseTasks);

            // ── PHASE 2: COPY FROM GLOBAL QUEUE ─────────────────────────────
            // N shared workers consume the queue in priority order.
            // Rules enforced across ALL jobs:
            //   1. Priority files go before non-priority files
            //   2. Heavy files (> threshold): only one at a time (_heavyFileSemaphore)
            //   3. Light files: full parallelism
            int workerCount;
            lock (_configLock) { workerCount = _maxSimultaneousJobs; }
            var workerTasks = Enumerable.Range(0, workerCount).Select(_ => Task.Run(() =>
            {
                while (!globalQueue.IsCompleted)
                {
                    if (!globalQueue.TryDequeueAny(out FileJob file))
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    CopyAndProcessFile(file);
                }
            })).ToArray();

            Task.WaitAll(workerTasks);

            // Finalize jobs that had files
            foreach (var (job, _) in validJobs)
            {
                _controlCoordinator.UnregisterJob(job.Name);
                if (jobsWithFiles.ContainsKey(job.Name))
                    _stateTracker.FinalizeJobState(job.Name);
            }

            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        // ================================================================
        //  SINGLE JOB EXECUTION — internal, used by unit tests only
        // ================================================================

        /// <summary>
        /// Executes a single job standalone. Used by unit tests.
        /// Production code uses <see cref="ExecuteBackup"/> with <see cref="GlobalFileQueue"/>.
        /// </summary>
        internal void ExecuteSingleJob(BackupJob job, int capturedIndex, ConcurrentBag<string> errors)
        {
            try
            {
                _stateTracker.UpdateJobState(job.Name, state => state.State = BackupState.Active);

                HashSet<string> priorityExtCopy;
                lock (_configLock) { priorityExtCopy = new HashSet<string>(_priorityExtensions, StringComparer.OrdinalIgnoreCase); }

                BackupStrategy strategy = job.Type == BackupType.Complete
                    ? new FullBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, priorityExtCopy)
                    : new DifferentialBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, priorityExtCopy);

                var files = strategy.Analyze();
                long totalSize = files.Sum(f => f.FileSize);

                _stateTracker.UpdateJobState(job.Name, state =>
                {
                    state.TotalFiles = files.Count;
                    state.TotalSize = totalSize;
                    state.RemainingFiles = files.Count;
                    state.RemainingSize = totalSize;
                });

                if (files.Count == 0) { _stateTracker.FinalizeJobState(job.Name); return; }

                strategy.Prepare();
                foreach (var file in files) CopyAndProcessFile(file);
                _stateTracker.FinalizeJobState(job.Name);
            }
            catch (Exception ex)
            {
                errors.Add($"Error executing '{job.Name}': {ex.Message}");
                _stateTracker.UpdateJobState(job.Name, state => state.State = BackupState.Error);
            }
        }

        // ================================================================
        //  FILE COPY AND PROCESSING
        // ================================================================

        /// <summary>
        /// Handles pause/cancellation checks and heavy-file mutual exclusion
        /// before delegating the actual copy to <see cref="PerformCopyAndLog"/>.
        /// </summary>
        private void CopyAndProcessFile(FileJob file)
        {
            if (_controlCoordinator.IsCancellationRequested(file.JobName)) return;

            var ct = _controlCoordinator.GetCancellationToken(file.JobName);

            // Wait for global and per-job resume
            try { _controlCoordinator.WaitForResume(file.JobName); }
            catch (OperationCanceledException) { return; }

            if (ct.IsCancellationRequested) return;

            // Pause if a business process is detected
            _processManager.WaitIfBusinessProcess(ct);
            if (ct.IsCancellationRequested) return;

            // Heavy file → acquire global mutex
            bool isHeavy = file.FileSize > _sizeThreshold;
            if (isHeavy)
            {
                try { _heavyFileSemaphore.Wait(ct); }
                catch (OperationCanceledException) { return; }

                try { PerformCopyAndLog(file); }
                finally { _heavyFileSemaphore.Release(); }
            }
            else
            {
                PerformCopyAndLog(file);
            }
        }

        /// <summary>
        /// Copies a file to its destination, optionally encrypts it,
        /// and writes a log record. Updates the job state after each file.
        /// </summary>
        protected virtual void PerformCopyAndLog(FileJob file)
        {
            try
            {
                string? destDir = Path.GetDirectoryName(file.DestinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                var stopwatch = Stopwatch.StartNew();
                File.Copy(file.SourcePath, file.DestinationPath, overwrite: true);
                stopwatch.Stop();

                long encryptionTime = file.IsEncrypted
                    ? EncryptionService.Instance.EncryptFile(file.DestinationPath)
                    : 0;

                _logManager.WriteLog(new Record
                {
                    Name = file.JobName,
                    Source = BackupLogManager.GetUncPath(file.SourcePath),
                    Target = BackupLogManager.GetUncPath(file.DestinationPath),
                    Size = file.FileSize,
                    Time = stopwatch.Elapsed.TotalMilliseconds,
                    Timestamp = DateTime.Now,
                    EncryptionTime = encryptionTime
                });

                _stateTracker.UpdateJobState(file.JobName, state =>
                {
                    state.RemainingFiles = Math.Max(0, state.RemainingFiles - 1);
                    state.RemainingSize = Math.Max(0, state.RemainingSize - file.FileSize);
                    state.CurrentSourceFile = file.SourcePath;
                    state.CurrentTargetFile = file.DestinationPath;
                });
            }
            catch (Exception)
            {
                _logManager.WriteLog(new Record
                {
                    Name = file.JobName,
                    Source = BackupLogManager.GetUncPath(file.SourcePath),
                    Target = BackupLogManager.GetUncPath(file.DestinationPath),
                    Size = file.FileSize,
                    Time = -1,
                    Timestamp = DateTime.Now,
                    EncryptionTime = 0
                });

                _stateTracker.UpdateJobState(file.JobName, state => state.State = BackupState.Error);
            }
        }
    }

}