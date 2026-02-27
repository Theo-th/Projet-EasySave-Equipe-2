using EasySave.Core.Models;
using EasySave.Core.Services;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Abstract class defining the backup analysis strategy.
    /// Analyze(): Phase 1 – read-only, never modifies disk.
    /// Prepare(): called by BackupService between Phase 2 and Phase 3
    ///            to prepare target folders (cleanup / creation).
    /// </summary>
    public abstract class BackupStrategy
    {
        protected const string FULL_FOLDER = "full";
        protected const string DIFFERENTIAL_FOLDER = "differential";
        protected const string DELETED_FILES_REPORT = "_deleted_files.txt";

        protected string SourceDirectory { get; }
        protected string TargetDirectory { get; }
        protected string JobName { get; }
        protected HashSet<string> PriorityExtensions { get; }

        protected BackupStrategy(string sourceDirectory, string targetDirectory,
            string jobName, HashSet<string> priorityExtensions)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            JobName = jobName;
            PriorityExtensions = priorityExtensions;
        }

        /// <summary>
        /// Analyzes the files to backup and returns the list of FileJobs.
        /// READ-ONLY operation: does not create, modify, or delete any files.
        /// Each FileJob contains: source, destination, job name, encryption boolean,
        /// priority boolean, and size – enabling sorting into 4 categories in Phase 2.
        /// </summary>
        public abstract List<FileJob> Analyze();

        /// <summary>
        /// Prepares destination folders before copy execution (Phase 3).
        /// Called by BackupService after Task.WaitAll of Phase 1.
        /// </summary>
        public abstract void Prepare();

        /// <summary>
        /// Validates the existence of the source directory.
        /// </summary>
        protected void ValidateSource()
        {
            if (!Directory.Exists(SourceDirectory))
                throw new DirectoryNotFoundException(
                    $"Source directory '{SourceDirectory}' does not exist.");
        }

        /// <summary>
        /// Creates a FileJob from a FileInfo.
        /// Automatically determines IsPriority (priority extension) and IsEncrypted.
        /// </summary>
        protected FileJob CreateFileJob(FileInfo file, string destinationFolder)
        {
            string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
            string extension = Path.GetExtension(file.FullName).ToLower();

            return new FileJob
            {
                SourcePath = file.FullName,
                DestinationPath = Path.Combine(destinationFolder, relativePath),
                JobName = JobName,
                IsEncrypted = EncryptionService.Instance.GetExtensions().Contains(extension),
                IsPriority = !string.IsNullOrEmpty(extension) && PriorityExtensions.Contains(extension),
                FileSize = file.Length
            };
        }

        /// <summary>
        /// Deletes the contents of a folder without deleting the folder itself.
        /// </summary>
        protected static void ClearFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            var dirInfo = new DirectoryInfo(folderPath);
            foreach (var file in dirInfo.GetFiles()) file.Delete();
            foreach (var subDir in dirInfo.GetDirectories()) subDir.Delete(recursive: true);
        }
    }
}