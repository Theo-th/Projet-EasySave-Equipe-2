using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services.Strategies
{
    public class DifferentialBackupStrategy : BackupStrategy
    {
        public DifferentialBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger, LogTarget logTarget)
            : base(sourceDirectory, targetDirectory, backupType, jobName, logger, logTarget)
        {
        }

        public override (bool Success, string? ErrorMessage) Execute()
        {
            var validation = ValidateAndPrepareDirectories();
            if (!validation.Success) return validation;

            try
            {
                string fullBackupFolder = Path.Combine(TargetDirectory, FULL_MARKER);
                if (!Directory.Exists(fullBackupFolder)) return ExecuteFullBackup(fullBackupFolder);

                string diffBackupFolder = Path.Combine(TargetDirectory, DIFFERENTIAL_MARKER);
                ClearBackupFolder(diffBackupFolder);

                var diffFolderCreation = CreateBackupFolder(diffBackupFolder, DIFFERENTIAL_MARKER);
                if (!diffFolderCreation.Success) return diffFolderCreation;

                var reportResult = CreateDeletedFilesReport(SourceDirectory, fullBackupFolder, diffBackupFolder);
                if (!reportResult.Success) return reportResult;

                var dirInfo = new DirectoryInfo(SourceDirectory);
                if (!dirInfo.Exists) return (false, $"Source directory '{SourceDirectory}' does not exist.");

                var sourceFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

                int modifiedCount = 0;
                long totalSize = 0;
                foreach (var sourceFile in sourceFiles)
                {
                    string relativePath = Path.GetRelativePath(SourceDirectory, sourceFile.FullName);
                    string fullBackupFilePath = Path.Combine(fullBackupFolder, relativePath);

                    if (!File.Exists(fullBackupFilePath) || sourceFile.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                    {
                        modifiedCount++;
                        totalSize += sourceFile.Length;
                    }
                }

                RaiseBackupInitialized(modifiedCount, totalSize);

                foreach (var sourceFile in sourceFiles)
                {
                    string relativePath = Path.GetRelativePath(SourceDirectory, sourceFile.FullName);
                    string fullBackupFilePath = Path.Combine(fullBackupFolder, relativePath);

                    if (!File.Exists(fullBackupFilePath) || sourceFile.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                    {
                        var copyResult = CopyFile(relativePath, SourceDirectory, diffBackupFolder);
                        if (!copyResult.Success) return copyResult;
                    }
                }

                return (true, null);
            }
            catch (Exception ex) { return (false, $"Error during differential backup: {ex.Message}"); }
        }

        private (bool Success, string? ErrorMessage) ExecuteFullBackup(string fullBackupFolder)
        {
            var folderCreation = CreateBackupFolder(fullBackupFolder, FULL_MARKER);
            if (!folderCreation.Success) return folderCreation;

            var dirInfo = new DirectoryInfo(SourceDirectory);
            if (!dirInfo.Exists) return (false, $"Source directory '{SourceDirectory}' does not exist.");

            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            long totalSize = allFiles.Sum(f => f.Length);
            RaiseBackupInitialized(allFiles.Length, totalSize);

            foreach (var file in allFiles)
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
                var copyResult = CopyFile(relativePath, SourceDirectory, fullBackupFolder);
                if (!copyResult.Success) return copyResult;
            }
            return (true, null);
        }

        private (bool Success, string? ErrorMessage) CreateDeletedFilesReport(string sourceDir, string fullBackupDir, string targetDir)
        {
            try
            {
                var backupFiles = new DirectoryInfo(fullBackupDir).GetFiles("*", SearchOption.AllDirectories);
                var deletedFiles = new List<string>();

                foreach (var backupFile in backupFiles)
                {
                    if (IsBackupMarker(backupFile.Name)) continue;
                    string relativePath = Path.GetRelativePath(fullBackupDir, backupFile.FullName);
                    string sourceFilePath = Path.Combine(sourceDir, relativePath);
                    if (!File.Exists(sourceFilePath)) deletedFiles.Add(relativePath);
                }

                if (deletedFiles.Count > 0)
                {
                    string reportPath = Path.Combine(targetDir, DELETED_FILES_REPORT);
                    File.WriteAllLines(reportPath, deletedFiles);
                }
                return (true, null);
            }
            catch (Exception ex) { return (false, $"Error detecting deleted files: {ex.Message}"); }
        }
    }
}