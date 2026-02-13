using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services.Strategies
{
    
    // Full backup strategy.
    public class FullBackupStrategy : BackupStrategy
    {
        public FullBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger)
            : base(sourceDirectory, targetDirectory, backupType, jobName, logger)
        {
        }

        /// <summary>
        /// Executes a full backup:
        /// 1. Validates source/destination directories
        /// 2. Traverses all source files and copies each one immediately
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
                
                // Clean previous content if it exists
                ClearBackupFolder(fullBackupFolder);

                var folderCreation = CreateBackupFolder(fullBackupFolder, FULL_MARKER);
                if (!folderCreation.Success)
                {
                    return folderCreation;
                }

                // Step 2: Traverse source and copy each file immediately
                var dirInfo = new DirectoryInfo(SourceDirectory);
                if (!dirInfo.Exists)
                {
                    return (false, $"Source directory '{SourceDirectory}' does not exist.");
                }

                var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

                // Compute total size and notify initialization
                long totalSize = allFiles.Sum(f => f.Length);
                RaiseBackupInitialized(allFiles.Length, totalSize);

                // Copy each file as it is encountered
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
            catch (Exception ex)
            {
                return (false, $"Error during full backup: {ex.Message}");
            }
        }
    }
}