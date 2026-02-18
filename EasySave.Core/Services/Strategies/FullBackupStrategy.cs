using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services.Strategies
{
    public class FullBackupStrategy : BackupStrategy
    {
        public FullBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger, LogTarget logTarget)
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
                ClearBackupFolder(fullBackupFolder);

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
            catch (Exception ex) { return (false, $"Error during full backup: {ex.Message}"); }
        }
    }
}