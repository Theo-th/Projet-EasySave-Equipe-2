using EasySave.Models;
using EasyLog;
using Projet_EasySave.Models;

namespace Projet_EasySave.Services.Strategies
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
        /// 3. Otherwise, clears the previous differential folder, lists modified files, reports deleted files, then copies
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

                // 3a: List modified files compared to the full backup
                List<string> modifiedFiles = ListModifiedFilesInSource(fullBackupFolder);

                // Compute total size and notify initialization
                long totalSize = ComputeTotalSize(modifiedFiles, SourceDirectory);
                RaiseBackupInitialized(modifiedFiles.Count, totalSize);

                // 3b: Generate the deleted files report
                var reportResult = CreateDeletedFilesReport(SourceDirectory, fullBackupFolder, diffBackupFolder);
                if (!reportResult.Success)
                {
                    return reportResult;
                }

                // 3c: Copy modified files from the list
                var copyResult = CopyFilesFromList(modifiedFiles, SourceDirectory, diffBackupFolder);
                if (!copyResult.Success)
                {
                    return copyResult;
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

            List<string> filesToCopy = ListAllFilesInSource();

            // Compute total size and notify initialization
            long totalSize = ComputeTotalSize(filesToCopy, SourceDirectory);
            RaiseBackupInitialized(filesToCopy.Count, totalSize);

            var copyResult = CopyFilesFromList(filesToCopy, SourceDirectory, fullBackupFolder);
            if (!copyResult.Success)
            {
                return copyResult;
            }

            return (true, null);
        }

        /// <summary>
        /// Lists all files in the source directory as relative paths.
        /// </summary>
        private List<string> ListAllFilesInSource()
        {
            var files = new List<string>();
            var dirInfo = new DirectoryInfo(SourceDirectory);

            if (!dirInfo.Exists)
                return files;

            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
                files.Add(relativePath);
            }

            return files;
        }

        /// <summary>
        /// Lists modified files in the source compared to the full backup.
        /// </summary>
        private List<string> ListModifiedFilesInSource(string fullBackupDir)
        {
            var modifiedFiles = new List<string>();
            var sourceFiles = new DirectoryInfo(SourceDirectory)
                .GetFiles("*", SearchOption.AllDirectories);

            foreach (var sourceFile in sourceFiles)
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, sourceFile.FullName);
                string fullBackupFilePath = Path.Combine(fullBackupDir, relativePath);

                // New or modified file
                if (!File.Exists(fullBackupFilePath) ||
                    sourceFile.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                {
                    modifiedFiles.Add(relativePath);
                }
            }

            return modifiedFiles;
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