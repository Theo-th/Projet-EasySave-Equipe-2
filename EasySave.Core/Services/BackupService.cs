using EasyLog;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;
using EasySave.Core.Services.Strategies;

namespace EasySave.Core.Services
{

    // Service for executing and managing backup operations.
    /// <summary>
    /// Manages backup operations and job execution.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly IBackupStateRepository _stateRepository;
        private readonly ProcessDetector _processDetector;
        private BaseLog _logger = null!; // Initialisé via ChangeLogFormat
        private string _logDirectory;

        // Currently active strategy (to allow cancellation, pause, resume)
        private BackupStrategy? _activeStrategy;
        private LogTarget _currentLogTarget = LogTarget.Both;

        // Semaphore pour limiter les travaux simultanés (Phase 1) - configurable
        private SemaphoreSlim _jobSemaphore;
        private int _maxSimultaneousJobs;

        // Seuil de taille pour différencier fichiers légers/lourds - configurable
        private long _sizeThreshold;

        // Extensions prioritaires (ex: .docx, .xlsx, .pdf)
        private HashSet<string> _priorityExtensions;

        // Lock pour éviter les modifications pendant une opération
        private readonly object _configLock = new object();

        // Event triggered on each backup job progress change.
        public event Action<BackupJobState>? OnProgressChanged;
        public event Action<string>? OnBusinessProcessDetected;

        public BackupService(IJobConfigService configService, IBackupStateRepository stateRepository, ProcessDetector processDetector, LogType logType, string? logDirectory = null, int maxSimultaneousJobs = 3, int fileSizeThresholdMB = 10, List<string>? priorityExtensions = null)
        {
            _configService = configService;
            _stateRepository = stateRepository;
            _processDetector = processDetector;
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            // Configuration des paramètres multi-threading
            _maxSimultaneousJobs = Math.Max(1, Math.Min(10, maxSimultaneousJobs)); // Limité entre 1 et 10
            _jobSemaphore = new SemaphoreSlim(_maxSimultaneousJobs, _maxSimultaneousJobs);
            _sizeThreshold = (long)fileSizeThresholdMB * 1_000_000; // Conversion MB en bytes
            // Configuration des extensions prioritaires
            _priorityExtensions = priorityExtensions != null 
                ? new HashSet<string>(priorityExtensions, StringComparer.OrdinalIgnoreCase) 
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Subscribe to process detection events to cancel active backup
            ChangeLogFormat(logType);
            _processDetector.ProcessStatusChanged += OnWatchedProcessStatusChanged;
        }

        // Method for defining the target
        public void SetLogTarget(LogTarget target)
        {
            _currentLogTarget = target;
        }

        private void OnWatchedProcessStatusChanged(object? sender, ProcessStatusChangedEventArgs e)
        {
            // Note: La mise en pause est maintenant gérée automatiquement dans CopyAndLogFile
            // On peut notifier l'interface si nécessaire
            if (e.IsRunning)
            {
                OnBusinessProcessDetected?.Invoke(e.Process.ProcessName);
            }
        }

        /// <summary>
        /// Pauses the currently active backup.
        /// </summary>
        public void PauseBackup() => _activeStrategy?.Pause();

        /// <summary>
        /// Resumes the currently paused backup.
        /// </summary>
        public void ResumeBackup() => _activeStrategy?.Resume();

        /// <summary>
        /// Stops (cancels) the currently active backup.
        /// </summary>
        public void StopBackup() => _activeStrategy?.Cancel();

        /// <summary>
        /// Changes the log format (JSON or XML).
        /// </summary>
        public void ChangeLogFormat(LogType logType)
        {
            _logger = logType == LogType.JSON 
                ? new JsonLog(_logDirectory) 
                : new XmlLog(_logDirectory);
        }

        /// <summary>
        /// Updates the logs directory path.
        /// </summary>
        public void UpdateLogsDirectory(string newLogsDirectory)
        {
            _logDirectory = newLogsDirectory;
            // Recreate logger with new directory
            ChangeLogFormat(_logger is JsonLog ? LogType.JSON : LogType.XML);
        }

        /// <summary>
        /// Updates multi-threading parameters dynamically.
        /// </summary>
        /// <param name="maxSimultaneousJobs">New maximum number of simultaneous jobs.</param>
        /// <param name="fileSizeThresholdMB">New file size threshold in MB.</param>
        public void UpdateThreadingSettings(int maxSimultaneousJobs, int fileSizeThresholdMB)
        {
            lock (_configLock)
            {
                // Update job limit
                _maxSimultaneousJobs = Math.Max(1, Math.Min(10, maxSimultaneousJobs));
                
                // Dispose old semaphore and create new one
                _jobSemaphore?.Dispose();
                _jobSemaphore = new SemaphoreSlim(_maxSimultaneousJobs, _maxSimultaneousJobs);
                
                // Update file size threshold
                _sizeThreshold = (long)fileSizeThresholdMB * 1_000_000;
            }
        }

        /// <summary>
        /// Updates priority extensions list dynamically.
        /// </summary>
        /// <param name="extensions">List of priority extensions (e.g., .docx, .pdf).</param>
        public void UpdatePriorityExtensions(List<string> extensions)
        {
            lock (_configLock)
            {
                _priorityExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the current list of priority extensions.
        /// </summary>
        public List<string> GetPriorityExtensions()
        {
            lock (_configLock)
            {
                return _priorityExtensions.ToList();
            }
        }

        // Executes backup jobs by their indices.
        // Returns a formatted backup status message or error message.
        public string? ExecuteBackup(List<int> jobIndices)
        {
            if (jobIndices == null || jobIndices.Count == 0) return "No backup job specified.";

            // Check if any watched business process is running before starting
            var runningProcess = _processDetector.IsAnyWatchedProcessRunning();
            if (runningProcess != null)
            {
                OnBusinessProcessDetected?.Invoke(runningProcess);
                return $"Backup aborted: business process '{runningProcess}' is currently running.";
            }

            var allJobs = _configService.GetAllJobs();
            var results = new List<string>();

            results.Add("===================================================================");
            results.Add("          EASYSAVE - MULTI-THREADING BACKUP SYSTEM");
            results.Add("===================================================================");
            results.Add("");

            // ========================================
            // PHASE 1 : ORDONNANCEMENT ET ANALYSE
            // ========================================
            results.Add("-------------------------------------------------------------------");
            results.Add("PHASE 1 : ORDONNANCEMENT ET ANALYSE");
            results.Add("Limitation: 3 travaux simultanes maximum");
            results.Add("Analyse: Full (tous) ou Differentielle (modifies)");
            results.Add("-------------------------------------------------------------------");
            results.Add("");

            // Listes globales pour fusionner les résultats de tous les travaux
            var globalPriorityLight = new List<FileJob>();
            var globalNonPriorityLight = new List<FileJob>();
            var globalPriorityHeavy = new List<FileJob>();
            var globalNonPriorityHeavy = new List<FileJob>();

            var analyseTasks = new List<Task>();
            var jobQueue = new Queue<int>(jobIndices);
            
            results.Add($"Nombre de travaux a traiter: {jobIndices.Count}");

            // Traitement avec limitation à 3 travaux simultanés
            while (jobQueue.Count > 0)
            {
                _jobSemaphore.Wait();
                
                if (jobQueue.Count == 0)
                {
                    _jobSemaphore.Release();
                    break;
                }

                int jobIndex = jobQueue.Dequeue();
                
                if (jobIndex < 0 || jobIndex >= allJobs.Count)
                {
                    _jobSemaphore.Release();
                    results.Add($"Invalid job index: {jobIndex}");
                    continue;
                }

                var job = allJobs[jobIndex];
                results.Add($"   Analyse du travail '{job.Name}' ({job.Type})...");

                // Création de la tâche d'analyse pour ce travail
                var analyseTask = Task.Run(() =>
                {
                    try
                    {
                        // Analyse et tri des fichiers selon le type de sauvegarde
                        var fileJobs = AnalyzeJob(job);

                        // Catégorisation des fichiers (priorité + taille)
                        FileJobCategorizer.CategorizeFiles(fileJobs, _sizeThreshold,
                            out var priorityLight, out var nonPriorityLight,
                            out var priorityHeavy, out var nonPriorityHeavy);

                        // Ajout aux listes globales (avec synchronisation)
                        lock (globalPriorityLight) { globalPriorityLight.AddRange(priorityLight); }
                        lock (globalNonPriorityLight) { globalNonPriorityLight.AddRange(nonPriorityLight); }
                        lock (globalPriorityHeavy) { globalPriorityHeavy.AddRange(priorityHeavy); }
                        lock (globalNonPriorityHeavy) { globalNonPriorityHeavy.AddRange(nonPriorityHeavy); }

                        var categorization = $"      - Prioritaires legers: {priorityLight.Count} | Non prioritaires legers: {nonPriorityLight.Count}";
                        var heavyInfo = $"      - Prioritaires lourds: {priorityHeavy.Count} | Non prioritaires lourds: {nonPriorityHeavy.Count}";
                        lock (results)
                        {
                            results.Add($"   OK '{job.Name}' -> {fileJobs.Count} fichiers trouves");
                            results.Add(categorization);
                            results.Add(heavyInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (results)
                        {
                            results.Add($"   ERREUR lors de l'analyse de '{job.Name}': {ex.Message}");
                        }
                    }
                    finally
                    {
                        _jobSemaphore.Release();
                    }
                });

                analyseTasks.Add(analyseTask);
            }

            // Attendre que toutes les analyses soient terminées
            Task.WaitAll(analyseTasks.ToArray());
            results.Add("");
            results.Add("Phase 1 terminee - Tous les travaux analyses");
            results.Add("");

            // ========================================
            // PHASE 2 : FUSION ET ATTRIBUTION DES RESSOURCES
            // ========================================
            results.Add("-------------------------------------------------------------------");
            results.Add("PHASE 2 : FUSION ET ATTRIBUTION DES RESSOURCES");
            results.Add("Creation de 3 files globales");
            results.Add("Ordre: Prioritaires legers -> Legers -> Lourds");
            results.Add("-------------------------------------------------------------------");
            results.Add("");

            // Création des trois files globales
            var queuePriorityLight = new Queue<FileJob>(globalPriorityLight);
            var queueNonPriorityLight = new Queue<FileJob>(globalNonPriorityLight);
            var queueHeavy = new Queue<FileJob>();
            
            // Fusion des fichiers lourds : prioritaires d'abord, puis non prioritaires
            foreach (var file in globalPriorityHeavy)
                queueHeavy.Enqueue(file);
            foreach (var file in globalNonPriorityHeavy)
                queueHeavy.Enqueue(file);

            var totalFiles = queuePriorityLight.Count + queueNonPriorityLight.Count + queueHeavy.Count;
            results.Add($"Categorisation globale ({totalFiles} fichiers au total):");
            results.Add($"   - File 1: Fichiers prioritaires legers    -> {queuePriorityLight.Count} fichiers");
            results.Add($"   - File 2: Fichiers legers standard        -> {queueNonPriorityLight.Count} fichiers");
            results.Add($"   - File 3: Fichiers lourds (>= 10 MB)      -> {queueHeavy.Count} fichiers");
            results.Add("");
            results.Add("Strategie de traitement:");
            results.Add($"   - Legers (< 10 MB)  : Multi-threading (parallele)");
            results.Add($"   - Lourds (>= 10 MB) : Mono-threading (sequentiel, evite saturation)");
            results.Add("");

            // ========================================
            // PHASE 3 : EXÉCUTION ET FINALISATION
            // ========================================
            results.Add("-------------------------------------------------------------------");
            results.Add("PHASE 3 : EXECUTION ET FINALISATION");
            results.Add("Copie + Chiffrement + Logs + Mise a jour state");
            results.Add("-------------------------------------------------------------------");
            results.Add("");

            // Semaphore pour limiter les fichiers lourds à 1 thread à la fois
            var heavySemaphore = new SemaphoreSlim(1, 1);
            var executionTasks = new List<Task>();
            var startTime = DateTime.Now;

            // 3.1 - Traitement des fichiers prioritaires légers (multi-thread)
            if (queuePriorityLight.Count > 0)
            {
                results.Add($"[Etape 3.1] Traitement des fichiers prioritaires legers ({queuePriorityLight.Count})");
                results.Add($"   Mode: Multi-threading (parallele)");
                var step1Start = DateTime.Now;
                
                while (queuePriorityLight.Count > 0)
                {
                    var file = queuePriorityLight.Dequeue();
                    executionTasks.Add(Task.Run(() => CopyAndLogFile(file)));
                }
                Task.WaitAll(executionTasks.ToArray());
                executionTasks.Clear();
                
                var step1Duration = (DateTime.Now - step1Start).TotalSeconds;
                results.Add($"   Termine en {step1Duration:F2}s");
                results.Add("");
            }

            // 3.2 - Traitement des fichiers légers standard (multi-thread)
            if (queueNonPriorityLight.Count > 0)
            {
                results.Add($"[Etape 3.2] Traitement des fichiers legers standard ({queueNonPriorityLight.Count})");
                results.Add($"   Mode: Multi-threading (parallele)");
                var step2Start = DateTime.Now;
                
                while (queueNonPriorityLight.Count > 0)
                {
                    var file = queueNonPriorityLight.Dequeue();
                    executionTasks.Add(Task.Run(() => CopyAndLogFile(file)));
                }
                Task.WaitAll(executionTasks.ToArray());
                executionTasks.Clear();
                
                var step2Duration = (DateTime.Now - step2Start).TotalSeconds;
                results.Add($"   Termine en {step2Duration:F2}s");
                results.Add("");
            }

            // 3.3 - Traitement des fichiers lourds (1 seul thread à la fois)
            if (queueHeavy.Count > 0)
            {
                results.Add($"[Etape 3.3] Traitement des fichiers lourds ({queueHeavy.Count})");
                results.Add($"   Mode: Mono-threading (1 fichier a la fois)");
                results.Add($"   Raison: Eviter la saturation de la bande passante");
                var step3Start = DateTime.Now;
                
                while (queueHeavy.Count > 0)
                {
                    var file = queueHeavy.Dequeue();
                    executionTasks.Add(Task.Run(() =>
                    {
                        heavySemaphore.Wait();
                        try
                        {
                            CopyAndLogFile(file);
                        }
                        finally
                        {
                            heavySemaphore.Release();
                        }
                    }));
                }
                Task.WaitAll(executionTasks.ToArray());
                
                var step3Duration = (DateTime.Now - step3Start).TotalSeconds;
                results.Add($"   Termine en {step3Duration:F2}s");
                results.Add("");
            }

            var totalDuration = (DateTime.Now - startTime).TotalSeconds;
            results.Add("===================================================================");
            results.Add("                    SAUVEGARDE TERMINEE");
            results.Add("===================================================================");
            results.Add("");
            results.Add($"{totalFiles} fichiers traites avec succes");
            results.Add($"Duree totale: {totalDuration:F2} secondes");
            results.Add($"Logs disponibles dans: {_logDirectory}");
            results.Add("");

            return string.Join("\n", results);
        }

        /// <summary>
        /// Analyse un travail de sauvegarde et retourne la liste des fichiers à copier.
        /// Pour les sauvegardes différentielles, vérifie qu'une sauvegarde complète existe.
        /// </summary>
        private List<FileJob> AnalyzeJob(BackupJob job)
        {
            var fileJobs = new List<FileJob>();
            var dirInfo = new DirectoryInfo(job.SourceDirectory);

            if (!dirInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            FileInfo[] filesToBackup;

            if (job.Type == BackupType.Complete)
            {
                // Sauvegarde complète : tous les fichiers
                filesToBackup = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            }
            else // BackupType.Differential
            {
                // Vérifier qu'une sauvegarde complète existe
                string fullBackupFolder = Path.Combine(job.TargetDirectory, "full");
                
                if (!Directory.Exists(fullBackupFolder))
                {
                    // Pas de full backup : on fait une sauvegarde complète comme référence
                    filesToBackup = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                }
                else
                {
                    // Sauvegarde différentielle : uniquement les fichiers modifiés
                    var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                    var modifiedFiles = new List<FileInfo>();

                    foreach (var file in allFiles)
                    {
                        string relativePath = Path.GetRelativePath(job.SourceDirectory, file.FullName);
                        string fullBackupFilePath = Path.Combine(fullBackupFolder, relativePath);

                        // Fichier ajouté ou modifié
                        if (!File.Exists(fullBackupFilePath) || 
                            file.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                        {
                            modifiedFiles.Add(file);
                        }
                    }

                    filesToBackup = modifiedFiles.ToArray();
                }
            }

            // Conversion en FileJob
            foreach (var file in filesToBackup)
            {
                string relativePath = Path.GetRelativePath(job.SourceDirectory, file.FullName);
                string destinationPath = job.Type == BackupType.Complete
                    ? Path.Combine(job.TargetDirectory, "full", relativePath)
                    : Path.Combine(job.TargetDirectory, "differential", relativePath);

                // Détection automatique de la priorité selon l'extension
                string extension = Path.GetExtension(file.FullName);
                bool isPriority = !string.IsNullOrEmpty(extension) && _priorityExtensions.Contains(extension);

                fileJobs.Add(new FileJob
                {
                    SourcePath = file.FullName,
                    DestinationPath = destinationPath,
                    JobName = job.Name,
                    IsEncrypted = false, // Peut être configuré via l'interface
                    IsPriority = isPriority,  // Détecté automatiquement selon l'extension
                    FileSize = file.Length
                });
            }

            return fileJobs;
        }

        /// <summary>
        /// Copie un fichier, gère le chiffrement, met à jour l'état et enregistre les logs.
        /// Met automatiquement en pause si un processus métier est détecté.
        /// </summary>
        private void CopyAndLogFile(FileJob file)
        {
            try
            {
                // PAUSE automatique si processus métier détecté (NON ARRET)
                string? runningProcess;
                while ((runningProcess = _processDetector.IsAnyWatchedProcessRunning()) != null)
                {
                    // Notification une seule fois
                    OnBusinessProcessDetected?.Invoke(runningProcess);
                    Thread.Sleep(1000); // Pause 1 seconde, puis re-teste
                }

                // Création du dossier cible si nécessaire
                var destDir = Path.GetDirectoryName(file.DestinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // Copie du fichier avec mesure du temps
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(file.SourcePath, file.DestinationPath, true);
                stopwatch.Stop();

                // Chiffrement si requis
                long encryptionTime = 0;
                if (file.IsEncrypted)
                {
                    encryptionTime = EncryptionService.Instance.EncryptFile(file.DestinationPath);
                }

                // Mise à jour des logs
                var record = new Record
                {
                    Name = file.JobName,
                    Source = file.SourcePath,
                    Target = file.DestinationPath,
                    Size = file.FileSize,
                    Time = stopwatch.Elapsed.TotalMilliseconds,
                    Timestamp = DateTime.Now,
                    EncryptionTime = encryptionTime
                };

                // Envoi des logs selon la cible configurée
                if (_currentLogTarget == LogTarget.Local || _currentLogTarget == LogTarget.Both)
                {
                    _logger.WriteLog(record);
                }
                
                if (_currentLogTarget == LogTarget.Server || _currentLogTarget == LogTarget.Both)
                {
                    _ = RemoteLogService.SendLogAsync(record);
                }

                // TODO: Mise à jour du state (BackupJobState) si nécessaire
                // OnProgressChanged?.Invoke(updatedState);
            }
            catch (Exception ex)
            {
                // Gestion d'erreur : log de l'échec
                var errorRecord = new Record
                {
                    Name = file.JobName,
                    Source = file.SourcePath,
                    Target = file.DestinationPath,
                    Size = file.FileSize,
                    Time = -1, // Indique une erreur
                    Timestamp = DateTime.Now,
                    EncryptionTime = 0
                };

                if (_currentLogTarget == LogTarget.Local || _currentLogTarget == LogTarget.Both)
                {
                    _logger.WriteLog(errorRecord);
                }
                
                if (_currentLogTarget == LogTarget.Server || _currentLogTarget == LogTarget.Both)
                {
                    _ = RemoteLogService.SendLogAsync(errorRecord);
                }

                // On peut aussi logger l'exception dans la console ou un fichier de log
                Console.WriteLine($"Error copying file {file.SourcePath}: {ex.Message}");
            }
        }
    }
}