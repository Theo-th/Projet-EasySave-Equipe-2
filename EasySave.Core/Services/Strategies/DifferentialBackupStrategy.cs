using EasySave.Core.Models;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Differential backup strategy.
    /// Analyze(): identifies added/modified files since the last full backup (read-only).
    /// If no full backup exists, analyzes all files (Full behavior).
    /// Prepare(): cleans the target folder and generates the deleted files report.
    /// </summary>
    public class DifferentialBackupStrategy : BackupStrategy
    {
        /// <summary>
        /// Remembers if the analysis behaved like a Full (no 'full' folder found).
        /// </summary>
        private bool _analyzedAsFull;

        public DifferentialBackupStrategy(string sourceDirectory, string targetDirectory,
            string jobName, HashSet<string> priorityExtensions)
            : base(sourceDirectory, targetDirectory, jobName, priorityExtensions)
        {
        }

        /// <summary>
        /// Analyzes files to be backed up (read-only, no disk modification).
        /// If the 'full/' folder does not exist: returns all files (to 'full/').
        /// Otherwise: returns only new or modified files (to 'differential/').
        /// </summary>
        public override List<FileJob> Analyze()
        {
            ValidateSource();

            string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);

            if (!Directory.Exists(fullBackupFolder))
            {
                _analyzedAsFull = true;
                return AnalyzeAllFiles(fullBackupFolder);
            }

            _analyzedAsFull = false;
            string diffBackupFolder = Path.Combine(TargetDirectory, DIFFERENTIAL_FOLDER);
            return AnalyzeChangedFiles(fullBackupFolder, diffBackupFolder);
        }

        /// <summary>
        /// Prepares the target folder according to the type of analysis performed.
        /// Also generates the deleted files report for a differential analysis.
        /// Called by BackupService just before Phase 3.
        /// </summary>
        public override void Prepare()
        {
            if (_analyzedAsFull)
            {
                string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);
                ClearFolder(fullBackupFolder);
                Directory.CreateDirectory(fullBackupFolder);
            }
            else
            {
                string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);
                string diffBackupFolder = Path.Combine(TargetDirectory, DIFFERENTIAL_FOLDER);
                ClearFolder(diffBackupFolder);
                Directory.CreateDirectory(diffBackupFolder);
                // Deleted files report only available here (after analysis, before copy)
                GenerateDeletedFilesReport(fullBackupFolder, diffBackupFolder);
            }
        }

        // ================================================================
        //  PRIVATE METHODS
        // ================================================================

        /// <summary>
        /// Full analysis: returns all source files to the 'full/' folder.
        /// </summary>
        private List<FileJob> AnalyzeAllFiles(string fullBackupFolder)
        {
            var dirInfo = new DirectoryInfo(SourceDirectory);
            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            var fileJobs = new List<FileJob>(allFiles.Length);
            foreach (var file in allFiles)
                fileJobs.Add(CreateFileJob(file, fullBackupFolder));

            return fileJobs;
        }

        /// <summary>
        /// Differential analysis: returns only added or modified files
        /// compared to the last full backup.
        /// </summary>
        private List<FileJob> AnalyzeChangedFiles(string fullBackupFolder, string diffBackupFolder)
        {
            var dirInfo = new DirectoryInfo(SourceDirectory);
            var sourceFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            var fileJobs = new List<FileJob>();
            foreach (var file in sourceFiles)
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
                string fullBackupFilePath = Path.Combine(fullBackupFolder, relativePath);

                bool isNew = !File.Exists(fullBackupFilePath);
                bool isModified = !isNew &&
                    file.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath);

                if (isNew || isModified)
                    fileJobs.Add(CreateFileJob(file, diffBackupFolder));
            }

            return fileJobs;
        }

        /// <summary>
        /// Generates a report of files present in the full backup
        /// but absent from the source (files deleted since the last Full).
        /// </summary>
        private void GenerateDeletedFilesReport(string fullBackupDir, string targetDir)
        {
            if (!Directory.Exists(fullBackupDir)) return;

            var backupFiles = new DirectoryInfo(fullBackupDir)
                .GetFiles("*", SearchOption.AllDirectories);

            var deletedFiles = new List<string>();
            foreach (var backupFile in backupFiles)
            {
                string relativePath = Path.GetRelativePath(fullBackupDir, backupFile.FullName);
                if (!File.Exists(Path.Combine(SourceDirectory, relativePath)))
                    deletedFiles.Add(relativePath);
            }

            if (deletedFiles.Count > 0)
                File.WriteAllLines(Path.Combine(targetDir, DELETED_FILES_REPORT), deletedFiles);
        }
    }
}