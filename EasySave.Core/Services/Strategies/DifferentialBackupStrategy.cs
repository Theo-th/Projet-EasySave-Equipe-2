using EasySave.Core.Models;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Stratégie de sauvegarde différentielle.
    /// Identifie les fichiers modifiés depuis la dernière sauvegarde complète.
    /// Si aucune sauvegarde complète n'existe, une sauvegarde complète est lancée à la place.
    /// </summary>
    public class DifferentialBackupStrategy : BackupStrategy
    {
        public DifferentialBackupStrategy(string sourceDirectory, string targetDirectory, string jobName, HashSet<string> priorityExtensions)
            : base(sourceDirectory, targetDirectory, jobName, priorityExtensions)
        {
        }

        /// <summary>
        /// Analyse les fichiers à sauvegarder.
        /// Vérifie d'abord qu'une sauvegarde complète existe, sinon analyse comme une complète.
        /// </summary>
        public override List<FileJob> Analyze()
        {
            ValidateDirectories();

            string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);

            // Si aucune sauvegarde complète n'existe, en créer une
            if (!Directory.Exists(fullBackupFolder))
                return AnalyzeAsFull(fullBackupFolder);

            return AnalyzeDifferential(fullBackupFolder);
        }

        /// <summary>
        /// Analyse comme une sauvegarde complète (quand aucune référence n'existe).
        /// </summary>
        private List<FileJob> AnalyzeAsFull(string fullBackupFolder)
        {
            Directory.CreateDirectory(fullBackupFolder);

            var dirInfo = new DirectoryInfo(SourceDirectory);
            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            var fileJobs = new List<FileJob>(allFiles.Length);
            foreach (var file in allFiles)
            {
                fileJobs.Add(CreateFileJob(file, fullBackupFolder));
            }

            return fileJobs;
        }

        /// <summary>
        /// Analyse différentielle : identifie les fichiers ajoutés ou modifiés
        /// par rapport à la dernière sauvegarde complète.
        /// </summary>
        private List<FileJob> AnalyzeDifferential(string fullBackupFolder)
        {
            string diffBackupFolder = Path.Combine(TargetDirectory, DIFFERENTIAL_FOLDER);
            ClearFolder(diffBackupFolder);
            Directory.CreateDirectory(diffBackupFolder);

            // Rapport des fichiers supprimés depuis la sauvegarde complète
            GenerateDeletedFilesReport(fullBackupFolder, diffBackupFolder);

            // Identifier les fichiers nouveaux ou modifiés
            var dirInfo = new DirectoryInfo(SourceDirectory);
            var sourceFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            var fileJobs = new List<FileJob>();
            foreach (var file in sourceFiles)
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
                string fullBackupFilePath = Path.Combine(fullBackupFolder, relativePath);

                // Fichier ajouté ou modifié
                if (!File.Exists(fullBackupFilePath) ||
                    file.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                {
                    fileJobs.Add(CreateFileJob(file, diffBackupFolder));
                }
            }

            return fileJobs;
        }

        /// <summary>
        /// Génère un rapport des fichiers supprimés (présents dans la sauvegarde complète
        /// mais absents de la source).
        /// </summary>
        private void GenerateDeletedFilesReport(string fullBackupDir, string targetDir)
        {
            var backupFiles = new DirectoryInfo(fullBackupDir).GetFiles("*", SearchOption.AllDirectories);
            var deletedFiles = new List<string>();

            foreach (var backupFile in backupFiles)
            {
                string relativePath = Path.GetRelativePath(fullBackupDir, backupFile.FullName);
                string sourceFilePath = Path.Combine(SourceDirectory, relativePath);
                if (!File.Exists(sourceFilePath))
                    deletedFiles.Add(relativePath);
            }

            if (deletedFiles.Count > 0)
            {
                string reportPath = Path.Combine(targetDir, DELETED_FILES_REPORT);
                File.WriteAllLines(reportPath, deletedFiles);
            }
        }
    }
}