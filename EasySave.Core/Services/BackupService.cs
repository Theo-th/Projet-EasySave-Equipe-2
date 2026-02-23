using EasyLog;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;
using EasySave.Core.Services.Strategies;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Service principal de sauvegarde multi-threadé.
    /// Orchestre les 3 phases : Analyse → Fusion → Exécution.
    /// Supporte le contrôle global ET par travail (pause/reprise/arrêt).
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly IBackupStateRepository _stateRepository;
        private readonly ProcessDetector _processDetector;
        private BaseLog _logger = null!;
        private string _logDirectory;
        private LogTarget _currentLogTarget = LogTarget.Both;

        // Configuration multi-threading
        private int _maxSimultaneousJobs;
        private SemaphoreSlim _jobSemaphore;
        private long _sizeThreshold;
        private HashSet<string> _priorityExtensions;
        private readonly object _configLock = new();

        // Mutex global : interdit deux transferts de fichiers LOURDS en simultané
        // (quelque soit le nombre de travaux actifs)
        private readonly SemaphoreSlim _heavyFileSemaphore = new SemaphoreSlim(1, 1);

        // Mécanisme de pause / annulation GLOBAL
        private readonly ManualResetEventSlim _pauseEvent = new(true);
        private CancellationTokenSource _cts = new();

        // Mécanisme de pause / annulation PAR TRAVAIL
        private readonly ConcurrentDictionary<string, ManualResetEventSlim> _jobPauseEvents = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobCancellations = new();

        // Suivi de l'état par travail (thread-safe)
        private readonly ConcurrentDictionary<string, BackupJobState> _jobStates = new();
        private readonly object _stateLock = new();

        // Lock dédié pour l'écriture des logs (EasyLog n'est pas thread-safe)
        private readonly object _logLock = new();

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
            _stateRepository = stateRepository;
            _processDetector = processDetector;
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            _maxSimultaneousJobs = Math.Clamp(maxSimultaneousJobs, 1, 10);
            _jobSemaphore = new SemaphoreSlim(_maxSimultaneousJobs, _maxSimultaneousJobs);
            _sizeThreshold = (long)fileSizeThresholdMB * 1_000_000;

            _priorityExtensions = priorityExtensions != null
                ? new HashSet<string>(priorityExtensions, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ChangeLogFormat(logType);
            _processDetector.ProcessStatusChanged += OnWatchedProcessStatusChanged;
        }

        // ================================================================
        //  CONFIGURATION
        // ================================================================

        public void SetLogTarget(LogTarget target) => _currentLogTarget = target;

        public void ChangeLogFormat(LogType logType)
        {
            _logger = logType == LogType.JSON
                ? new JsonLog(_logDirectory)
                : new XmlLog(_logDirectory);
        }

        public void UpdateLogsDirectory(string newLogsDirectory)
        {
            _logDirectory = newLogsDirectory;
            ChangeLogFormat(_logger is JsonLog ? LogType.JSON : LogType.XML);
        }

        public void UpdateThreadingSettings(int maxSimultaneousJobs, int fileSizeThresholdMB)
        {
            lock (_configLock)
            {
                _maxSimultaneousJobs = Math.Clamp(maxSimultaneousJobs, 1, 10);
                _jobSemaphore?.Dispose();
                _jobSemaphore = new SemaphoreSlim(_maxSimultaneousJobs, _maxSimultaneousJobs);
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
            _pauseEvent.Reset();
            UpdateAllJobStates(BackupState.Active, BackupState.Paused);
        }

        public void ResumeBackup()
        {
            _pauseEvent.Set();
            foreach (var ev in _jobPauseEvents.Values) ev.Set();
            UpdateAllJobStates(BackupState.Paused, BackupState.Active);
        }

        public void StopBackup()
        {
            _cts.Cancel();
            _pauseEvent.Set();
            foreach (var ev in _jobPauseEvents.Values) ev.Set();
            UpdateAllJobStates(BackupState.Active, BackupState.Inactive);
            UpdateAllJobStates(BackupState.Paused, BackupState.Inactive);
        }

        // ================================================================
        //  CONTRÔLE PAR TRAVAIL INDIVIDUEL
        // ================================================================

        public void PauseJob(string jobName)
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var ev)) ev.Reset();

            if (_jobStates.TryGetValue(jobName, out var state))
            {
                state.State = BackupState.Paused;
                state.LastActionTimestamp = DateTime.Now;
                OnProgressChanged?.Invoke(state);
                SaveStates();
            }
        }

        public void ResumeJob(string jobName)
        {
            if (_jobPauseEvents.TryGetValue(jobName, out var ev)) ev.Set();

            if (_jobStates.TryGetValue(jobName, out var state))
            {
                state.State = BackupState.Active;
                state.LastActionTimestamp = DateTime.Now;
                OnProgressChanged?.Invoke(state);
                SaveStates();
            }
        }

        public void StopJob(string jobName)
        {
            if (_jobCancellations.TryGetValue(jobName, out var cts)) cts.Cancel();
            if (_jobPauseEvents.TryGetValue(jobName, out var ev)) ev.Set();

            if (_jobStates.TryGetValue(jobName, out var state))
            {
                state.State = BackupState.Inactive;
                state.LastActionTimestamp = DateTime.Now;
                OnProgressChanged?.Invoke(state);
                SaveStates();
            }
        }

        // ================================================================
        //  EXÉCUTION PRINCIPALE
        // ================================================================

        public string? ExecuteBackup(List<int> jobIndices)
        {
            if (jobIndices == null || jobIndices.Count == 0)
                return "No backup job specified.";

            // Réinitialisation complète
            _cts = new CancellationTokenSource();
            _pauseEvent.Set();
            _jobStates.Clear();
            foreach (var ev in _jobPauseEvents.Values) ev.Dispose();
            foreach (var cts in _jobCancellations.Values) cts.Dispose();
            _jobPauseEvents.Clear();
            _jobCancellations.Clear();

            var allJobs = _configService.GetAllJobs();
            var errors = new ConcurrentBag<string>();

            // Collecter les travaux valides
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

            // ============================================================
            // PRÉ-ENREGISTREMENT
            // Tous les travaux sont enregistrés en Inactive dès le départ.
            // Les travaux au-delà de _maxSimultaneousJobs restent Inactive
            // (en attente) jusqu'à ce qu'un slot se libère.
            // ============================================================
            foreach (var (job, index) in validJobs)
            {
                _jobPauseEvents[job.Name] = new ManualResetEventSlim(true);
                _jobCancellations[job.Name] = new CancellationTokenSource();

                var waitingState = new BackupJobState
                {
                    Id = index + 1,
                    Name = job.Name,
                    SourcePath = job.SourceDirectory,
                    TargetPath = job.TargetDirectory,
                    Type = job.Type,
                    State = BackupState.Inactive   // En attente de slot
                };
                _jobStates[job.Name] = waitingState;
                OnProgressChanged?.Invoke(waitingState);
            }
            SaveStates();

            // ============================================================
            // LANCEMENT
            // Chaque travail tourne dans sa propre Task.
            // _jobSemaphore est acquis ET TENU pour toute la durée du travail
            // (analyse + copie + finalisation).
            // Le 4ème travail bloque sur Wait() jusqu'à la fin complète
            // d'un des 3 premiers.
            // ============================================================
            var tasks = validJobs
                .Select(entry => Task.Run(() =>
                {
                    var (job, capturedIndex) = entry;

                    // Attente du slot — bloque si _maxSimultaneousJobs sont déjà actifs
                    _jobSemaphore.Wait();
                    try
                    {
                        ExecuteSingleJob(job, capturedIndex, errors);
                    }
                    finally
                    {
                        // Libère le slot seulement après copie + finalisation complètes
                        _jobSemaphore.Release();
                    }
                }))
                .ToArray();

            Task.WaitAll(tasks);
            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        // ================================================================
        //  EXÉCUTION D'UN SEUL TRAVAIL (3 PHASES)
        // ================================================================

        /// <summary>
        /// Exécute un travail complet en 3 phases (Analyse → Catégorisation → Copie).
        /// Appelé depuis ExecuteBackup dans un Task.Run protégé par _jobSemaphore.
        /// </summary>
        private void ExecuteSingleJob(BackupJob job, int capturedIndex, ConcurrentBag<string> errors)
        {
            try
            {
                // ---- PHASE 1 : ANALYSE (lecture seule) ----
                var state = _jobStates[job.Name];
                state.State = BackupState.Active;
                state.LastActionTimestamp = DateTime.Now;
                OnProgressChanged?.Invoke(state);
                SaveStates();

                BackupStrategy strategy = job.Type == BackupType.Complete
                    ? new FullBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, _priorityExtensions)
                    : new DifferentialBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, _priorityExtensions);

                var files = strategy.Analyze();

                long totalSize = files.Sum(f => f.FileSize);
                state.TotalFiles = files.Count;
                state.TotalSize = totalSize;
                state.RemainingFiles = files.Count;
                state.RemainingSize = totalSize;
                state.LastActionTimestamp = DateTime.Now;
                OnProgressChanged?.Invoke(state);
                SaveStates();

                if (files.Count == 0)
                {
                    FinalizeJobState(job.Name);
                    return;
                }

                // ---- PHASE 2 : CATÉGORISATION ----
                // Tri en 4 catégories ; les lourds sont fusionnés en file unique
                // (prioritaires en tête).
                FileJobCategorizer.CategorizeFiles(
                    files, _sizeThreshold,
                    out var priorityLight, out var nonPriorityLight,
                    out var priorityHeavy, out var nonPriorityHeavy);

                var heavyFiles = new List<FileJob>(priorityHeavy.Count + nonPriorityHeavy.Count);
                heavyFiles.AddRange(priorityHeavy);
                heavyFiles.AddRange(nonPriorityHeavy);

                // ---- PRÉPARATION DES DOSSIERS CIBLES ----
                strategy.Prepare();

                // ---- PHASE 3 : COPIE ----

                // Fichiers lourds : thread unique dédié.
                // _heavyFileSemaphore garantit qu'un seul fichier lourd est
                // transféré à la fois sur l'ensemble des travaux actifs.
                var heavyTask = Task.Run(() =>
                {
                    foreach (var file in heavyFiles)
                    {
                        if (IsCancelledJob(job.Name)) break;
                        CopyAndProcessFile(file);
                    }
                });

                // Fichiers prioritaires légers : multi-thread
                if (priorityLight.Count > 0 && !IsCancelledJob(job.Name))
                {
                    try
                    {
                        Parallel.ForEach(priorityLight,
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = Environment.ProcessorCount,
                                CancellationToken = BuildLinkedToken(job.Name)
                            },
                            file => CopyAndProcessFile(file));
                    }
                    catch (OperationCanceledException) { }
                }

                // Fichiers légers standard : multi-thread, uniquement après les prioritaires
                if (nonPriorityLight.Count > 0 && !IsCancelledJob(job.Name))
                {
                    try
                    {
                        Parallel.ForEach(nonPriorityLight,
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = Environment.ProcessorCount,
                                CancellationToken = BuildLinkedToken(job.Name)
                            },
                            file => CopyAndProcessFile(file));
                    }
                    catch (OperationCanceledException) { }
                }

                try { heavyTask.Wait(); }
                catch (AggregateException) { }

                FinalizeJobState(job.Name);
            }
            catch (Exception ex)
            {
                errors.Add($"Error executing '{job.Name}': {ex.Message}");
                if (_jobStates.TryGetValue(job.Name, out var errState))
                {
                    errState.State = BackupState.Error;
                    OnProgressChanged?.Invoke(errState);
                    SaveStates();
                }
            }
        }

        // ================================================================
        //  COPIE ET TRAITEMENT D'UN FICHIER
        // ================================================================

        private void CopyAndProcessFile(FileJob file)
        {
            // 1. Annulation globale ou par travail
            if (_cts.IsCancellationRequested) return;
            _jobCancellations.TryGetValue(file.JobName, out var jobCts);
            if (jobCts?.IsCancellationRequested == true) return;

            // Token combiné (global + travail) pour toutes les attentes
            using var linkedCts = jobCts != null
                ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, jobCts.Token)
                : CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            var ct = linkedCts.Token;

            // 2. Attente pause globale
            try { _pauseEvent.Wait(ct); }
            catch (OperationCanceledException) { return; }

            // 3. Attente pause par travail
            if (_jobPauseEvents.TryGetValue(file.JobName, out var jobPauseEvent) && !jobPauseEvent.IsSet)
            {
                try { jobPauseEvent.Wait(ct); }
                catch (OperationCanceledException) { return; }
            }

            if (ct.IsCancellationRequested) return;

            // 4. Pause si processus métier détecté (pause, pas arrêt)
            WaitIfBusinessProcess(ct);
            if (ct.IsCancellationRequested) return;

            // 5. Fichier lourd → mutex global (1 seul transfert lourd à la fois)
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

        // Changer 'private' → 'protected virtual' pour permettre l'override dans les tests
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

                WriteLog(new Record
                {
                    Name = file.JobName,
                    Source = GetUncPath(file.SourcePath),
                    Target = GetUncPath(file.DestinationPath),
                    Size = file.FileSize,
                    Time = stopwatch.Elapsed.TotalMilliseconds,
                    Timestamp = DateTime.Now,
                    EncryptionTime = encryptionTime
                });

                UpdateJobProgress(file);
            }
            catch (Exception)
            {
                WriteLog(new Record
                {
                    Name = file.JobName,
                    Source = GetUncPath(file.SourcePath),
                    Target = GetUncPath(file.DestinationPath),
                    Size = file.FileSize,
                    Time = -1,
                    Timestamp = DateTime.Now,
                    EncryptionTime = 0
                });

                if (_jobStates.TryGetValue(file.JobName, out var state))
                {
                    state.State = BackupState.Error;
                    OnProgressChanged?.Invoke(state);
                    SaveStates();
                }
            }
        }

        // ================================================================
        //  MÉTHODES UTILITAIRES
        // ================================================================

        private bool IsCancelledJob(string jobName)
        {
            if (_cts.IsCancellationRequested) return true;
            return _jobCancellations.TryGetValue(jobName, out var cts) && cts.IsCancellationRequested;
        }

        private CancellationToken BuildLinkedToken(string jobName)
        {
            _jobCancellations.TryGetValue(jobName, out var jobCts);
            return jobCts != null
                ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, jobCts.Token).Token
                : _cts.Token;
        }

        /// <summary>
        /// Met la sauvegarde en pause si un processus métier est détecté.
        /// Attente interruptible de 500 ms via le CancellationToken.
        /// Reprend automatiquement quand le processus métier s'arrête.
        /// </summary>
        private void WaitIfBusinessProcess(CancellationToken ct)
        {
            string? runningProcess;
            bool wasPaused = false;

            while (!ct.IsCancellationRequested &&
                   (runningProcess = _processDetector.IsAnyWatchedProcessRunning()) != null)
            {
                if (!wasPaused)
                {
                    wasPaused = true;
                    OnBusinessProcessDetected?.Invoke(runningProcess);
                    UpdateAllJobStates(BackupState.Active, BackupState.Paused);
                }
                ct.WaitHandle.WaitOne(500);
            }

            if (wasPaused && !ct.IsCancellationRequested)
                UpdateAllJobStates(BackupState.Paused, BackupState.Active);
        }

        private void UpdateJobProgress(FileJob file)
        {
            if (!_jobStates.TryGetValue(file.JobName, out var state)) return;

            lock (_stateLock)
            {
                state.RemainingFiles = Math.Max(0, state.RemainingFiles - 1);
                state.RemainingSize = Math.Max(0, state.RemainingSize - file.FileSize);
                state.CurrentSourceFile = file.SourcePath;
                state.CurrentTargetFile = file.DestinationPath;
                state.LastActionTimestamp = DateTime.Now;
            }

            OnProgressChanged?.Invoke(state);
            SaveStates();
        }

        private void UpdateAllJobStates(BackupState fromState, BackupState toState)
        {
            foreach (var state in _jobStates.Values)
            {
                if (state.State == fromState)
                {
                    state.State = toState;
                    state.LastActionTimestamp = DateTime.Now;
                    OnProgressChanged?.Invoke(state);
                }
            }
            SaveStates();
        }

        /// <summary>
        /// Finalise l'état d'un travail individuel après complétion ou annulation.
        /// </summary>
        private void FinalizeJobState(string jobName)
        {
            if (!_jobStates.TryGetValue(jobName, out var state)) return;

            if (state.State == BackupState.Active || state.State == BackupState.Paused)
            {
                state.State = state.RemainingFiles == 0
                    ? BackupState.Completed
                    : BackupState.Inactive;
                state.LastActionTimestamp = DateTime.Now;
            }

            OnProgressChanged?.Invoke(state);
            SaveStates();
        }

        private void SaveStates()
        {
            lock (_stateLock)
            {
                _stateRepository.UpdateState(_jobStates.Values.ToList());
            }
        }

        private void WriteLog(Record record)
        {
            if (_currentLogTarget == LogTarget.Local || _currentLogTarget == LogTarget.Both)
            {
                lock (_logLock) { _logger.WriteLog(record); }
            }

            if (_currentLogTarget == LogTarget.Server || _currentLogTarget == LogTarget.Both)
                _ = RemoteLogService.SendLogAsync(record);
        }

        private static string GetUncPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith(@"\\")) return path;
            if (path.Length >= 2 && path[1] == ':')
                return $@"\\{Environment.MachineName}\{path[0]}${path.Substring(2)}";
            return path;
        }

        private void OnWatchedProcessStatusChanged(object? sender, ProcessStatusChangedEventArgs e)
        {
            if (e.IsRunning)
                OnBusinessProcessDetected?.Invoke(e.Process.ProcessName);
        }
    }
}