using EasySave.Core.Models;
using EasySave.Core.Services;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Classe abstraite définissant la stratégie d'analyse de sauvegarde.
    /// Analyze() : Phase 1 — lecture seule, ne modifie jamais le disque.
    /// Prepare() : appelé par BackupService entre la Phase 2 et la Phase 3
    ///             pour préparer les dossiers cibles (nettoyage / création).
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
        /// Analyse les fichiers à sauvegarder et retourne la liste des FileJob.
        /// Opération en LECTURE SEULE : ne crée, ne modifie ni ne supprime aucun fichier.
        /// Chaque FileJob contient : source, destination, nom du travail, booléen chiffrement,
        /// booléen priorité et taille — permettant le tri en 4 catégories en Phase 2.
        /// </summary>
        public abstract List<FileJob> Analyze();

        /// <summary>
        /// Prépare les dossiers de destination avant l'exécution des copies (Phase 3).
        /// Appelé par BackupService après Task.WaitAll de la Phase 1.
        /// </summary>
        public abstract void Prepare();

        /// <summary>
        /// Valide l'existence du répertoire source.
        /// </summary>
        protected void ValidateSource()
        {
            if (!Directory.Exists(SourceDirectory))
                throw new DirectoryNotFoundException(
                    $"Le répertoire source '{SourceDirectory}' n'existe pas.");
        }

        /// <summary>
        /// Crée un FileJob à partir d'un FileInfo.
        /// Détermine automatiquement IsPriority (extension prioritaire) et IsEncrypted.
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