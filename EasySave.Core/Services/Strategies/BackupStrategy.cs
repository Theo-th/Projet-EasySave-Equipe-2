using EasySave.Core.Models;
using EasyLog;
using System.Diagnostics;
using EasySave.Core.Services;

namespace EasySave.Core.Services.Strategies
{
    // Abstract class defining the backup strategy.
    public abstract class BackupStrategy
    {
        // Constants for marker files
        protected const string FULL_MARKER = "full";
        protected const string DIFFERENTIAL_MARKER = "differential";
        protected const string DELETED_FILES_REPORT = "_deleted_files.txt";

        protected string SourceDirectory { get; set; }
        protected string TargetDirectory { get; set; }
        protected BackupType BackupType { get; set; }
        protected string JobName { get; set; }
        protected BaseLog Logger { get; set; }
        protected LogTarget _logTarget;

        // Cancellation token to stop the backup in progress
        private CancellationTokenSource _cancellationTokenSource = new();

        // Pause mechanism: starts in signaled state (not paused)
        private readonly ManualResetEventSlim _pauseEvent = new(true);

        public event Action<int, long>? OnBackupInitialized;

        // Event triggered after each file transfer (sourceFile, targetFile, fileSize).
        public event Action<string, string, long>? OnFileTransferred;
        public event Action<bool>? OnPauseStateChanged;

        public BackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger, LogTarget logTarget)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            BackupType = backupType;
            JobName = jobName;
            Logger = logger;
            _logTarget = logTarget;
        }

        // Executes the backup strategy.
        public abstract (bool Success, string? ErrorMessage) Execute();

        /// <summary>
        /// Requests cancellation of the current backup operation.
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
            // Unblock if paused so the cancellation can be processed
            _pauseEvent.Set();
        }

        public void Pause()
        {
            _pauseEvent.Reset();
            OnPauseStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Resumes a paused backup operation.
        /// </summary>
        public void Resume()
        {
            _pauseEvent.Set();
            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Returns true if the backup is currently paused.
        /// </summary>
        public bool IsPaused => !_pauseEvent.IsSet;

        /// <summary>
        /// Checks whether cancellation has been requested and throws if so.
        /// Call this before each file copy to allow interruption.
        /// </summary>
        protected void ThrowIfCancellationRequested()
        {
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Waits if the backup is paused, then checks for cancellation.
        /// Call this before each file copy to support pause and stop.
        /// </summary>
        protected void WaitIfPausedAndThrowIfCancelled()
        {
            // Block here if paused
            _pauseEvent.Wait(_cancellationTokenSource.Token);
            // After unblocking, check if we should cancel
            ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Returns true if cancellation has been requested.
        /// </summary>
        protected bool IsCancellationRequested => _cancellationTokenSource.Token.IsCancellationRequested;

        // Validates and prepares the source and destination directories.
        protected (bool Success, string? ErrorMessage) ValidateAndPrepareDirectories()
        {
            if (!Directory.Exists(SourceDirectory)) return (false, $"Source directory '{SourceDirectory}' does not exist.");
            try
            {
                if (!Directory.Exists(TargetDirectory)) Directory.CreateDirectory(TargetDirectory);
                return (true, null);
            }
            catch (Exception ex) { return (false, $"Unable to create the destination directory: {ex.Message}"); }
        }

        // Checks whether a file is a backup marker.
        protected bool IsBackupMarker(string fileName)
        {
            return fileName == FULL_MARKER || fileName == DIFFERENTIAL_MARKER;
        }

        // Creates a backup folder and its marker file.
        protected (bool Success, string? ErrorMessage) CreateBackupFolder(string backupFolderPath, string markerFileName)
        {
            try
            {
                Directory.CreateDirectory(backupFolderPath);
                string markerFilePath = Path.Combine(backupFolderPath, markerFileName);
                string markerContent = $"Backup {markerFileName} created on {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                var stopwatch = Stopwatch.StartNew();
                File.WriteAllText(markerFilePath, markerContent);
                stopwatch.Stop();

                var record = new Record
                {
                    Name = JobName,
                    Source = "",
                    Target = markerFilePath,
                    Size = markerContent.Length,
                    Time = stopwatch.Elapsed.TotalMilliseconds,
                    Timestamp = DateTime.Now
                };

                if (_logTarget == LogTarget.Local || _logTarget == LogTarget.Both)
                    Logger.WriteLog(record);

                if (_logTarget == LogTarget.Server || _logTarget == LogTarget.Both)
                    _ = RemoteLogService.SendLogAsync(record);

                return (true, null);
            }
            catch (Exception ex) { return (false, $"Error creating the backup folder: {ex.Message}"); }
        }

        // Computes the total size of a list of files relative to a source directory.
        protected long ComputeTotalSize(List<string> relativeFilePaths, string sourceDir)
        {
            long totalSize = 0;
            foreach (string relativePath in relativeFilePaths)
            {
                var fileInfo = new FileInfo(Path.Combine(sourceDir, relativePath));
                if (fileInfo.Exists) totalSize += fileInfo.Length;
            }
            return totalSize;
        }

        // Raises the backup initialization event.
        protected void RaiseBackupInitialized(int totalFiles, long totalSize)
        {
            OnBackupInitialized?.Invoke(totalFiles, totalSize);
        }

        // Raises the file transfer event.
        protected void RaiseFileTransferred(string sourceFile, string targetFile, long fileSize)
        {
            OnFileTransferred?.Invoke(sourceFile, targetFile, fileSize);
        }

        /// <summary>
        /// Copies a single file (relative path) from a source directory to a target directory.
        /// The transfer is logged via Logger.WriteLog() and triggers a progress event.
        /// </summary>
        protected (bool Success, string? ErrorMessage) CopyFile(
            string relativePath, string sourceDir, string targetDir)
        {
            try
            {
                // Wait if paused, then check cancellation before each file copy
                WaitIfPausedAndThrowIfCancelled();
                string sourceFilePath = Path.Combine(sourceDir, relativePath);
                string targetFilePath = Path.Combine(targetDir, relativePath);

                // Create necessary subdirectories
                string? targetFileDir = Path.GetDirectoryName(targetFilePath);
                if (targetFileDir != null && !Directory.Exists(targetFileDir)) Directory.CreateDirectory(targetFileDir);

                var fileInfo = new FileInfo(sourceFilePath);
                long fileSize = fileInfo.Length;
                var stopwatch = Stopwatch.StartNew();
                File.Copy(sourceFilePath, targetFilePath, overwrite: true);
                stopwatch.Stop();

                long encryptionTime = EncryptionService.Instance.EncryptFile(targetFilePath);

                var record = new Record
                {
                    Name = JobName,
                    Source = sourceFilePath,
                    Target = targetFilePath,
                    Size = fileSize,
                    Time = stopwatch.Elapsed.TotalMilliseconds,
                    Timestamp = DateTime.Now,
                    EncryptionTime = encryptionTime,
                };

                if (_logTarget == LogTarget.Local || _logTarget == LogTarget.Both)
                    Logger.WriteLog(record);

                if (_logTarget == LogTarget.Server || _logTarget == LogTarget.Both)
                    _ = RemoteLogService.SendLogAsync(record);

                // Notify progress after file copied
                RaiseFileTransferred(sourceFilePath, targetFilePath, fileSize);
                return (true, null);
            }
            catch (OperationCanceledException) { return (false, $"Backup cancelled."); }
            catch (Exception ex) { return (false, $"Error copying file '{relativePath}': {ex.Message}"); }
        }

        // Clears the contents of a backup folder without deleting the folder itself.
        protected void ClearBackupFolder(string backupFolder)
        {
            try
            {
                if (Directory.Exists(backupFolder))
                {
                    var dirInfo = new DirectoryInfo(backupFolder);
                    // Delete all files
                    foreach (var file in dirInfo.GetFiles()) file.Delete();
                    // Delete all subdirectories
                    foreach (var subDir in dirInfo.GetDirectories()) subDir.Delete(recursive: true);
                }
                else Directory.CreateDirectory(backupFolder);
            }
            catch (Exception ex) { throw new IOException($"Error clearing the backup folder: {ex.Message}", ex); }
        }
    }
}