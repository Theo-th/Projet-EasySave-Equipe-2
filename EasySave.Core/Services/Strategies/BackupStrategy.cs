using EasySave.Core.Models;
using EasySave.Core.Services;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Classe abstraite définissant la stratégie d'analyse de sauvegarde.
    /// Responsable uniquement de l'analyse des fichiers (Phase 1).
    /// La copie, le chiffrement, les logs et l'état sont gérés par BackupService.
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

        protected BackupStrategy(string sourceDirectory, string targetDirectory, string jobName, HashSet<string> priorityExtensions)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            JobName = jobName;
            PriorityExtensions = priorityExtensions;
        }

        /// <summary>
        /// Analyse les fichiers à sauvegarder et retourne la liste des FileJob.
        /// Chaque FileJob contient : source, destination, nom du travail, booléen chiffrement.
        /// </summary>
        public abstract List<FileJob> Analyze();

        /// <summary>
        /// Valide l'existence du répertoire source et crée le répertoire cible si nécessaire.
        /// </summary>
        protected void ValidateDirectories()
        {
            if (!Directory.Exists(SourceDirectory))
                throw new DirectoryNotFoundException($"Le répertoire source '{SourceDirectory}' n'existe pas.");

            if (!Directory.Exists(TargetDirectory))
                Directory.CreateDirectory(TargetDirectory);
        }

        /// <summary>
        /// Crée un FileJob à partir d'un FileInfo et d'un dossier de destination.
        /// Détermine automatiquement la priorité (selon l'extension) et le chiffrement.
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
        /// Supprime le contenu d'un dossier sans supprimer le dossier lui-même.
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