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

        // Configuration multi-threading
        private int _maxSimultaneousJobs;
        private long _sizeThreshold;
        private HashSet<string> _priorityExtensions;
        private readonly object _configLock = new();

        // Global mutex: prevents two HEAVY file transfers simultaneously
        private readonly SemaphoreSlim _heavyFileSemaphore = new SemaphoreSlim(1, 1);

        // Events
        public event Action<BackupJobState>? OnProgressChanged;
        public event Action<string>? OnBusinessProcessDetected;

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

        public void SetLogTarget(LogTarget target) => _logManager.SetLogTarget(target);
        public void ChangeLogFormat(LogType logType) => _logManager.ChangeLogFormat(logType);
        public void UpdateLogsDirectory(string newLogsDirectory) => _logManager.UpdateLogsDirectory(newLogsDirectory);

        public void UpdateThreadingSettings(int maxSimultaneousJobs, int fileSizeThresholdMB)
        {
            lock (_configLock)
            {
                _maxSimultaneousJobs = Math.Clamp(maxSimultaneousJobs, 1, 10);
                _sizeThreshold = (long)fileSizeThresholdMB * 1_000_000;
            }
        }

        public void UpdatePriorityExtensions(List<string> extensions)
        {
            lock (_configLock)
            {
                _priorityExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            }
        }

        public List<string> GetPriorityExtensions()
        {
            lock (_configLock) { return _priorityExtensions.ToList(); }
        }

        // ================================================================
        //  CONTRÔLE GLOBAL (TOUS LES TRAVAUX)
        // ================================================================

        public void PauseBackup()
        {
            _controlCoordinator.PauseAllJobs();
            _stateTracker.UpdateAllJobStates(BackupState.Active, BackupState.Paused);
        }

        public void ResumeBackup()
        {
            _controlCoordinator.ResumeAllJobs();
            _stateTracker.UpdateAllJobStates(BackupState.Paused, BackupState.Active);
        }

        public void StopBackup()
        {
            _controlCoordinator.StopAllJobs();
            _stateTracker.UpdateAllJobStates(BackupState.Active, BackupState.Inactive);
            _stateTracker.UpdateAllJobStates(BackupState.Paused, BackupState.Inactive);
        }

        // ================================================================
        //  CONTRÔLE PAR TRAVAIL INDIVIDUEL
        // ================================================================

        public void PauseJob(string jobName)
        {
            _controlCoordinator.PauseJob(jobName);
            _stateTracker.UpdateJobState(jobName, state => state.State = BackupState.Paused);
        }

        public void ResumeJob(string jobName)
        {
            _controlCoordinator.ResumeJob(jobName);
            _stateTracker.UpdateJobState(jobName, state => state.State = BackupState.Active);
        }

        public void StopJob(string jobName)
        {
            _controlCoordinator.StopJob(jobName);
            _stateTracker.UpdateJobState(jobName, state => state.State = BackupState.Inactive);
        }

        // ================================================================
        //  EXÉCUTION PRINCIPALE
        // ================================================================

        public string? ExecuteBackup(List<int> jobIndices)
        {
            if (jobIndices == null || jobIndices.Count == 0)
                return "No backup job specified.";

            // Réinitialisation complète
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

            // ── PHASE 1 : ANALYSE EN PARALLÈLE ─────────────────────────────
            // Chaque job analyse ses fichiers et les dépose dans la file globale.
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

                    // Enregistrement comme producteur avant d'envoyer les fichiers
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

            // ── PHASE 2 : COPIE DEPUIS LA FILE GLOBALE ──────────────────────
            // N workers communs consomment la file dans l'ordre de priorité.
            // Règles respectées entre TOUS les jobs :
            //   1. Fichiers prioritaires passent devant les non-prioritaires
            //   2. Fichiers lourds (> seuil) : un seul à la fois (_heavyFileSemaphore)
            //   3. Fichiers légers : parallélisme complet
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

            // Finaliser les jobs qui avaient des fichiers
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
        /// Production code uses ExecuteBackup with GlobalFileQueue.
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

        private void CopyAndProcessFile(FileJob file)
        {
            if (_controlCoordinator.IsCancellationRequested(file.JobName)) return;

            var ct = _controlCoordinator.GetCancellationToken(file.JobName);

            // Wait for global and per-job resume
            try { _controlCoordinator.WaitForResume(file.JobName); }
            catch (OperationCanceledException) { return; }

            if (ct.IsCancellationRequested) return;

            // Pause if business process detected
            _processManager.WaitIfBusinessProcess(ct);
            if (ct.IsCancellationRequested) return;

            // Heavy file → global mutex
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