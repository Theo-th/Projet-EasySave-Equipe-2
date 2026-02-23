using EasySave.Core.Models;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Stratégie de sauvegarde différentielle.
    /// Analyze() : identifie les fichiers ajoutés/modifiés depuis la dernière sauvegarde complète (lecture seule).
    ///             Si aucune sauvegarde complète n'existe, analyse tous les fichiers (comportement Full).
    /// Prepare() : nettoie le dossier cible et génère le rapport des fichiers supprimés.
    /// </summary>
    public class DifferentialBackupStrategy : BackupStrategy
    {
        // Mémorise si l'analyse s'est comportée comme une Full (aucun dossier 'full' trouvé).
        private bool _analyzedAsFull;

        public DifferentialBackupStrategy(string sourceDirectory, string targetDirectory,
            string jobName, HashSet<string> priorityExtensions)
            : base(sourceDirectory, targetDirectory, jobName, priorityExtensions)
        {
        }

        /// <summary>
        /// Analyse les fichiers à sauvegarder (lecture seule, aucune modification disque).
        /// — Si le dossier 'full/' n'existe pas : retourne tous les fichiers (vers 'full/').
        /// — Sinon : retourne uniquement les fichiers nouveaux ou modifiés (vers 'differential/').
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
        /// Prépare le dossier cible selon le type d'analyse effectuée.
        /// Génère également le rapport des fichiers supprimés pour une analyse différentielle.
        /// Appelé par BackupService juste avant la Phase 3.
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
                // Rapport des suppressions uniquement disponible ici (après analyse, avant copie)
                GenerateDeletedFilesReport(fullBackupFolder, diffBackupFolder);
            }
        }

        // ----------------------------------------------------------------
        //  MÉTHODES PRIVÉES
        // ----------------------------------------------------------------

        /// <summary>
        /// Analyse complète : retourne tous les fichiers source vers le dossier 'full/'.
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
        /// Analyse différentielle : retourne uniquement les fichiers ajoutés ou modifiés
        /// par rapport à la dernière sauvegarde complète.
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
        /// Génère un rapport des fichiers présents dans la sauvegarde complète
        /// mais absents de la source (fichiers supprimés depuis la dernière Full).
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