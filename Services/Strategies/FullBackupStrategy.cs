using EasySave.Models;
using EasyLog;
using Projet_EasySave.Models;

namespace Projet_EasySave.Services.Strategies
{
    /// <summary>
    /// Full backup strategy.
    /// </summary>
    public class FullBackupStrategy : BackupStrategy
    {
        public FullBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger)
            : base(sourceDirectory, targetDirectory, backupType, jobName, logger)
        {
        }

        /// <summary>
        /// Executes a full backup:
        /// 1. Validates source/destination directories
        /// 2. Lists all files to copy
        /// 3. Copies files from the list
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

                // Step 2: List all files to copy
                List<string> filesToCopy = ListAllFilesInSource();

                // Compute total size and notify initialization
                long totalSize = ComputeTotalSize(filesToCopy, SourceDirectory);
                RaiseBackupInitialized(filesToCopy.Count, totalSize);

                // Step 3: Copy files from the list
                var copyResult = CopyFilesFromList(filesToCopy, SourceDirectory, fullBackupFolder);
                if (!copyResult.Success)
                {
                    return copyResult;
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error during full backup: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively lists all files in the source directory as relative paths.
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
    }
}