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

        // Mécanisme de pause / annulation global
        private readonly ManualResetEventSlim _pauseEvent = new(true);
        private CancellationTokenSource _cts = new();

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
        //  CONTRÔLE : PAUSE / RESUME / STOP
        // ================================================================

        public void PauseBackup()
        {
            _pauseEvent.Reset();
            UpdateAllJobStates(BackupState.Active, BackupState.Paused);
        }

        public void ResumeBackup()
        {
            _pauseEvent.Set();
            UpdateAllJobStates(BackupState.Paused, BackupState.Active);
        }

        public void StopBackup()
        {
            _cts.Cancel();
            _pauseEvent.Set(); // Débloquer si en pause pour traiter l'annulation
            UpdateAllJobStates(BackupState.Active, BackupState.Inactive);
            UpdateAllJobStates(BackupState.Paused, BackupState.Inactive);
        }

        // ================================================================
        //  EXÉCUTION PRINCIPALE (3 PHASES)
        // ================================================================

        public string? ExecuteBackup(List<int> jobIndices)
        {
            if (jobIndices == null || jobIndices.Count == 0)
                return "No backup job specified.";

            // Réinitialisation de l'état global
            _cts = new CancellationTokenSource();
            _pauseEvent.Set();
            _jobStates.Clear();

            var allJobs = _configService.GetAllJobs();
            var errors = new ConcurrentBag<string>();

            // ============================================================
            // PHASE 1 : ORDONNANCEMENT ET ANALYSE
            // Limite : _maxSimultaneousJobs threads simultanés (défaut 3)
            // Chaque travail est instancié selon son type (Full / Différentielle)
            // Les fichiers sont triés en 4 catégories via FileJob
            // ============================================================

            var allFileJobs = new ConcurrentBag<FileJob>();
            var analyseTasks = new List<Task>();

            foreach (var jobIndex in jobIndices)
            {
                if (jobIndex < 0 || jobIndex >= allJobs.Count)
                {
                    errors.Add($"Invalid job index: {jobIndex}");
                    continue;
                }

                var job = allJobs[jobIndex];
                int capturedIndex = jobIndex;

                _jobSemaphore.Wait();

                var task = Task.Run(() =>
                {
                    try
                    {
                        // Instanciation selon le type de sauvegarde
                        BackupStrategy strategy = job.Type == BackupType.Complete
                            ? new FullBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, _priorityExtensions)
                            : new DifferentialBackupStrategy(job.SourceDirectory, job.TargetDirectory, job.Name, _priorityExtensions);

                        // Analyse des fichiers
                        var files = strategy.Analyze();

                        // Initialisation de l'état du travail
                        long totalSize = files.Sum(f => f.FileSize);
                        var state = new BackupJobState
                        {
                            Id = capturedIndex + 1,
                            Name = job.Name,
                            SourcePath = job.SourceDirectory,
                            TargetPath = job.TargetDirectory,
                            Type = job.Type,
                            State = BackupState.Active,
                            TotalFiles = files.Count,
                            TotalSize = totalSize,
                            RemainingFiles = files.Count,
                            RemainingSize = totalSize
                        };

                        _jobStates[job.Name] = state;
                        OnProgressChanged?.Invoke(state);
                        SaveStates();

                        // Ajout aux fichiers globaux pour la phase 2
                        foreach (var f in files)
                            allFileJobs.Add(f);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error analyzing '{job.Name}': {ex.Message}");
                        _jobStates[job.Name] = new BackupJobState
                        {
                            Id = capturedIndex + 1,
                            Name = job.Name,
                            SourcePath = job.SourceDirectory,
                            TargetPath = job.TargetDirectory,
                            Type = job.Type,
                            State = BackupState.Error
                        };
                    }
                    finally
                    {
                        _jobSemaphore.Release();
                    }
                });

                analyseTasks.Add(task);
            }

            // Attendre la fin de toutes les analyses
            Task.WaitAll(analyseTasks.ToArray());

            if (allFileJobs.IsEmpty)
            {
                FinalizeStates();
                return errors.Count > 0 ? string.Join("\n", errors) : "No files to backup.";
            }

            // ============================================================
            // PHASE 2 : FUSION ET ATTRIBUTION DES RESSOURCES
            // Les 4 catégories de chaque travail sont fusionnées en 3 files :
            //   1. Fichiers prioritaires légers
            //   2. Fichiers légers standard
            //   3. Fichiers lourds (prioritaires d'abord, puis non prioritaires)
            // ============================================================

            FileJobCategorizer.CategorizeFiles(
                allFileJobs, _sizeThreshold,
                out var priorityLight, out var nonPriorityLight,
                out var priorityHeavy, out var nonPriorityHeavy);

            // File 3 : fusion des lourds (prioritaires en tête)
            var heavyFiles = new List<FileJob>(priorityHeavy.Count + nonPriorityHeavy.Count);
            heavyFiles.AddRange(priorityHeavy);
            heavyFiles.AddRange(nonPriorityHeavy);

            // ============================================================
            // PHASE 3 : EXÉCUTION ET FINALISATION
            //
            // Contraintes :
            //   - Fichiers lourds  : 1 seul thread dédié (éviter saturation bande passante)
            //   - Fichiers légers  : multi-thread (parallèle)
            //   - Non prioritaires : traités uniquement après la fin des prioritaires
            //
            // Pour chaque fichier :
            //   1. Copie
            //   2. Chiffrement (si requis via EncryptionService)
            //   3. Mise à jour de l'état (BackupJobState + event OnProgressChanged)
            //   4. Écriture des logs (via la DLL EasyLog)
            //
            // Si un processus métier est détecté → pause (pas arrêt)
            // ============================================================

            // Thread unique dédié aux fichiers lourds (tourne en parallèle des légers)
            var heavyTask = Task.Run(() =>
            {
                foreach (var file in heavyFiles)
                {
                    if (_cts.IsCancellationRequested) break;
                    CopyAndProcessFile(file);
                }
            });

            // Fichiers prioritaires légers (multi-thread)
            if (priorityLight.Count > 0 && !_cts.IsCancellationRequested)
            {
                Parallel.ForEach(priorityLight,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    file => CopyAndProcessFile(file));
            }

            // Fichiers légers standard (multi-thread, uniquement après les prioritaires)
            if (nonPriorityLight.Count > 0 && !_cts.IsCancellationRequested)
            {
                Parallel.ForEach(nonPriorityLight,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    file => CopyAndProcessFile(file));
            }

            // Attendre la fin du thread lourd
            try { heavyTask.Wait(); }
            catch (AggregateException) { /* Annulation gérée en interne */ }

            // Finalisation de l'état de tous les travaux
            FinalizeStates();

            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        // ================================================================
        //  COPIE ET TRAITEMENT D'UN FICHIER
        // ================================================================

        /// <summary>
        /// Copie un fichier, chiffre si nécessaire, met à jour l'état et enregistre les logs.
        /// Gère la pause utilisateur et la pause automatique (processus métier).
        /// </summary>
        private void CopyAndProcessFile(FileJob file)
        {
            // Vérification de l'annulation
            if (_cts.IsCancellationRequested) return;

            // Attente si en pause (utilisateur)
            try { _pauseEvent.Wait(_cts.Token); }
            catch (OperationCanceledException) { return; }

            // Attente si un processus métier est détecté (pause automatique, pas arrêt)
            WaitForBusinessProcess();

            // Re-vérifier l'annulation après les pauses
            if (_cts.IsCancellationRequested) return;

            try
            {
                // Création du répertoire cible si nécessaire
                string? destDir = Path.GetDirectoryName(file.DestinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                // Copie du fichier avec mesure du temps
                var stopwatch = Stopwatch.StartNew();
                File.Copy(file.SourcePath, file.DestinationPath, overwrite: true);
                stopwatch.Stop();

                // Chiffrement si requis (appel de l'app CryptoSoft via EncryptionService)
                long encryptionTime = 0;
                if (file.IsEncrypted)
                    encryptionTime = EncryptionService.Instance.EncryptFile(file.DestinationPath);

                // Écriture des logs (via la DLL EasyLog)
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

                // Mise à jour de l'état du travail (event + state.json)
                UpdateJobProgress(file);
            }
            catch (Exception ex)
            {
                // Log de l'erreur
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

        /// <summary>
        /// Attend que tous les processus métier surveillés soient terminés.
        /// Les sauvegardes sont mises en PAUSE (pas arrêtées) pendant la détection.
        /// </summary>
        private void WaitForBusinessProcess()
        {
            string? runningProcess;
            bool wasPaused = false;

            while ((runningProcess = _processDetector.IsAnyWatchedProcessRunning()) != null)
            {
                if (!wasPaused)
                {
                    OnBusinessProcessDetected?.Invoke(runningProcess);
                    UpdateAllJobStates(BackupState.Active, BackupState.Paused);
                    wasPaused = true;
                }

                Thread.Sleep(1000);
                if (_cts.IsCancellationRequested) return;
            }

            if (wasPaused)
                UpdateAllJobStates(BackupState.Paused, BackupState.Active);
        }

        /// <summary>
        /// Met à jour la progression d'un travail après la copie d'un fichier.
        /// </summary>
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

        /// <summary>
        /// Met à jour l'état de tous les travaux correspondant à un état source.
        /// </summary>
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
        /// Finalise l'état de tous les travaux à la fin de l'exécution.
        /// </summary>
        private void FinalizeStates()
        {
            foreach (var state in _jobStates.Values)
            {
                if (state.State == BackupState.Active || state.State == BackupState.Paused)
                {
                    state.State = state.RemainingFiles == 0
                        ? BackupState.Completed
                        : BackupState.Inactive;
                    state.LastActionTimestamp = DateTime.Now;
                }
                OnProgressChanged?.Invoke(state);
            }
            SaveStates();
        }

        /// <summary>
        /// Persiste l'état de tous les travaux via le repository (state.json).
        /// </summary>
        private void SaveStates()
        {
            lock (_stateLock)
            {
                _stateRepository.UpdateState(_jobStates.Values.ToList());
            }
        }

        /// <summary>
        /// Enregistre un log de manière thread-safe.
        /// EasyLog (JsonLog/XmlLog) n'est pas thread-safe : un lock est requis
        /// pour éviter les accès concurrents au fichier de log.
        /// </summary>
        private void WriteLog(Record record)
        {
            if (_currentLogTarget == LogTarget.Local || _currentLogTarget == LogTarget.Both)
            {
                lock (_logLock)
                {
                    _logger.WriteLog(record);
                }
            }

            if (_currentLogTarget == LogTarget.Server || _currentLogTarget == LogTarget.Both)
                _ = RemoteLogService.SendLogAsync(record);
        }

        /// <summary>
        /// Convertit un chemin local en chemin UNC réseau.
        /// </summary>
        private static string GetUncPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith(@"\\")) return path;
            if (path.Length >= 2 && path[1] == ':')
                return $@"\\{Environment.MachineName}\{path[0]}${path.Substring(2)}";
            return path;
        }

        /// <summary>
        /// Gère l'événement de changement de statut d'un processus surveillé.
        /// </summary>
        private void OnWatchedProcessStatusChanged(object? sender, ProcessStatusChangedEventArgs e)
        {
            if (e.IsRunning)
                OnBusinessProcessDetected?.Invoke(e.Process.ProcessName);
        }
    }
}