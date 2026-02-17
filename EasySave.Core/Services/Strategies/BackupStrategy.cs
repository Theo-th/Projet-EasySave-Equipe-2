using EasySave.Core.Models;
using EasyLog;
using System.Diagnostics;
using EasySave.Core.Services;

namespace EasySave.Core.Services.Strategies
{
    public abstract class BackupStrategy
    {
        protected const string FULL_MARKER = "full";
        protected const string DIFFERENTIAL_MARKER = "differential";
        protected const string DELETED_FILES_REPORT = "_deleted_files.txt";

        protected string SourceDirectory { get; set; }
        protected string TargetDirectory { get; set; }
        protected BackupType BackupType { get; set; }
        protected string JobName { get; set; }
        protected BaseLog Logger { get; set; }
        protected LogTarget _logTarget;

        private CancellationTokenSource _cancellationTokenSource = new();
        private readonly ManualResetEventSlim _pauseEvent = new(true);

        public event Action<int, long>? OnBackupInitialized;
        public event Action<string, string, long>? OnFileTransferred;
        public event Action<bool>? OnPauseStateChanged;

        // Constructeur modifié
        public BackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger, LogTarget logTarget)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            BackupType = backupType;
            JobName = jobName;
            Logger = logger;
            _logTarget = logTarget;
        }

        public abstract (bool Success, string? ErrorMessage) Execute();

        public void Cancel() { _cancellationTokenSource.Cancel(); _pauseEvent.Set(); }
        public void Pause() { _pauseEvent.Reset(); OnPauseStateChanged?.Invoke(true); }
        public void Resume() { _pauseEvent.Set(); OnPauseStateChanged?.Invoke(false); }
        public bool IsPaused => !_pauseEvent.IsSet;

        protected void ThrowIfCancellationRequested() => _cancellationTokenSource.Token.ThrowIfCancellationRequested();

        protected void WaitIfPausedAndThrowIfCancelled()
        {
            _pauseEvent.Wait(_cancellationTokenSource.Token);
            ThrowIfCancellationRequested();
        }

        protected bool IsCancellationRequested => _cancellationTokenSource.Token.IsCancellationRequested;

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

        protected bool IsBackupMarker(string fileName) => fileName == FULL_MARKER || fileName == DIFFERENTIAL_MARKER;

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

        protected void RaiseBackupInitialized(int totalFiles, long totalSize) => OnBackupInitialized?.Invoke(totalFiles, totalSize);
        protected void RaiseFileTransferred(string sourceFile, string targetFile, long fileSize) => OnFileTransferred?.Invoke(sourceFile, targetFile, fileSize);

        protected (bool Success, string? ErrorMessage) CopyFile(string relativePath, string sourceDir, string targetDir)
        {
            try
            {
                WaitIfPausedAndThrowIfCancelled();
                string sourceFilePath = Path.Combine(sourceDir, relativePath);
                string targetFilePath = Path.Combine(targetDir, relativePath);

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

                // GESTION DU LOG TARGET
                if (_logTarget == LogTarget.Local || _logTarget == LogTarget.Both)
                    Logger.WriteLog(record);

                if (_logTarget == LogTarget.Server || _logTarget == LogTarget.Both)
                    _ = RemoteLogService.SendLogAsync(record);

                RaiseFileTransferred(sourceFilePath, targetFilePath, fileSize);
                return (true, null);
            }
            catch (OperationCanceledException) { return (false, $"Backup cancelled."); }
            catch (Exception ex) { return (false, $"Error copying file '{relativePath}': {ex.Message}"); }
        }

        protected void ClearBackupFolder(string backupFolder)
        {
            try
            {
                if (Directory.Exists(backupFolder))
                {
                    var dirInfo = new DirectoryInfo(backupFolder);
                    foreach (var file in dirInfo.GetFiles()) file.Delete();
                    foreach (var subDir in dirInfo.GetDirectories()) subDir.Delete(recursive: true);
                }
                else Directory.CreateDirectory(backupFolder);
            }
            catch (Exception ex) { throw new IOException($"Error clearing the backup folder: {ex.Message}", ex); }
        }
    }
}