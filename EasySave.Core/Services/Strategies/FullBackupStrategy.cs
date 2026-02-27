using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Full backup strategy.
    /// Analyze(): scans all source files (read-only).
    /// Prepare(): cleans and recreates the 'full' folder before copying.
    /// </summary>
    public class FullBackupStrategy : BackupStrategy
    {
        public FullBackupStrategy(string sourceDirectory, string targetDirectory,
            string jobName, HashSet<string> priorityExtensions)
            : base(sourceDirectory, targetDirectory, jobName, priorityExtensions)
        {
        }

        /// <summary>
        /// Scans all source files and returns FileJobs with destination in 'full/'.
        /// Does not touch the disk: no file or folder creation or deletion.
        /// </summary>
        public override List<FileJob> Analyze()
        {
            ValidateSource();

            string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);

            var dirInfo = new DirectoryInfo(SourceDirectory);
            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            var fileJobs = new List<FileJob>(allFiles.Length);
            foreach (var file in allFiles)
                fileJobs.Add(CreateFileJob(file, fullBackupFolder));

            return fileJobs;
        }

        /// <summary>
        /// Cleans and recreates the full backup folder.
        /// Called by BackupService just before Phase 3.
        /// </summary>
        public override void Prepare()
        {
            string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);
            ClearFolder(fullBackupFolder);
            Directory.CreateDirectory(fullBackupFolder);
        }
    }
}