using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Differential backup strategy (modified files only).
    /// </summary>
    public class DifferentialBackupStrategy : BackupStrategy
    {
        public DifferentialBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger)
            : base(sourceDirectory, targetDirectory, backupType, jobName, logger)
        {
        }

        /// <summary>
        /// Executes a differential backup:
        /// 1. Validates source/destination directories
        /// 2. If no full backup exists, performs one
        /// 3. Otherwise, clears the previous differential folder, traverses source files,
        ///    copies modified ones immediately, and reports deleted files
        /// </summary>
        public override (bool Success, string? ErrorMessage) Execute()
        {
            // Step 1: Directory validation
            var validation = ValidateAndPrepareDirectories();
            if (!validation.Success)
            {
                return validation;
            }

            try
            {
                string fullBackupFolder = Path.Combine(TargetDirectory, FULL_MARKER);

                // Step 2: Check if a full backup exists
                if (!Directory.Exists(fullBackupFolder))
                {
                    return ExecuteFullBackup(fullBackupFolder);
                }

                // Step 3: Clear the contents of the previous differential folder
                string diffBackupFolder = Path.Combine(TargetDirectory, DIFFERENTIAL_MARKER);
                ClearBackupFolder(diffBackupFolder);

                var diffFolderCreation = CreateBackupFolder(diffBackupFolder, DIFFERENTIAL_MARKER);
                if (!diffFolderCreation.Success)
                {
                    return diffFolderCreation;
                }

                // 3a: Generate the deleted files report
                var reportResult = CreateDeletedFilesReport(SourceDirectory, fullBackupFolder, diffBackupFolder);
                if (!reportResult.Success)
                {
                    return reportResult;
                }

                // 3b: Traverse source files, compare with full backup, and copy modified ones immediately
                var dirInfo = new DirectoryInfo(SourceDirectory);
                if (!dirInfo.Exists)
                {
                    return (false, $"Source directory '{SourceDirectory}' does not exist.");
                }

                var sourceFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

                // Pre-compute modified files count and size for initialization event
                int modifiedCount = 0;
                long totalSize = 0;
                foreach (var sourceFile in sourceFiles)
                {
                    string relativePath = Path.GetRelativePath(SourceDirectory, sourceFile.FullName);
                    string fullBackupFilePath = Path.Combine(fullBackupFolder, relativePath);

                    if (!File.Exists(fullBackupFilePath) ||
                        sourceFile.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                    {
                        modifiedCount++;
                        totalSize += sourceFile.Length;
                    }
                }

                RaiseBackupInitialized(modifiedCount, totalSize);

                // Now traverse again and copy each modified file immediately
                foreach (var sourceFile in sourceFiles)
                {
                    string relativePath = Path.GetRelativePath(SourceDirectory, sourceFile.FullName);
                    string fullBackupFilePath = Path.Combine(fullBackupFolder, relativePath);

                    if (!File.Exists(fullBackupFilePath) ||
                        sourceFile.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                    {
                        var copyResult = CopyFile(relativePath, SourceDirectory, diffBackupFolder);
                        if (!copyResult.Success)
                        {
                            return copyResult;
                        }
                    }
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error during differential backup: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs an initial full backup when none exists.
        /// </summary>
        private (bool Success, string? ErrorMessage) ExecuteFullBackup(string fullBackupFolder)
        {
            var folderCreation = CreateBackupFolder(fullBackupFolder, FULL_MARKER);
            if (!folderCreation.Success)
            {
                return folderCreation;
            }

            var dirInfo = new DirectoryInfo(SourceDirectory);
            if (!dirInfo.Exists)
            {
                return (false, $"Source directory '{SourceDirectory}' does not exist.");
            }

            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            // Compute total size and notify initialization
            long totalSize = allFiles.Sum(f => f.Length);
            RaiseBackupInitialized(allFiles.Length, totalSize);

            foreach (var file in allFiles)
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);

                var copyResult = CopyFile(relativePath, SourceDirectory, fullBackupFolder);
                if (!copyResult.Success)
                {
                    return copyResult;
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Detects deleted files (present in the full backup but missing from the source)
        /// and generates a report in the destination folder.
        /// </summary>
        private (bool Success, string? ErrorMessage) CreateDeletedFilesReport(
            string sourceDir, string fullBackupDir, string targetDir)
        {
            try
            {
                var backupFiles = new DirectoryInfo(fullBackupDir)
                    .GetFiles("*", SearchOption.AllDirectories);

                var deletedFiles = new List<string>();

                foreach (var backupFile in backupFiles)
                {
                    if (IsBackupMarker(backupFile.Name))
                        continue;

                    string relativePath = Path.GetRelativePath(fullBackupDir, backupFile.FullName);
                    string sourceFilePath = Path.Combine(sourceDir, relativePath);

                    if (!File.Exists(sourceFilePath))
                    {
                        deletedFiles.Add(relativePath);
                    }
                }

                if (deletedFiles.Count > 0)
                {
                    string reportPath = Path.Combine(targetDir, DELETED_FILES_REPORT);
                    File.WriteAllLines(reportPath, deletedFiles);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error detecting deleted files: {ex.Message}");
            }
        }
    }
}