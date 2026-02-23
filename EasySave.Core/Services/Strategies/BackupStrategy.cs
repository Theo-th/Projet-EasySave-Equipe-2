using EasySave.Core.Models;
using EasySave.Core.Services;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Classe abstraite d�finissant la strat�gie d'analyse de sauvegarde.
    /// Analyze() : Phase 1 � lecture seule, ne modifie jamais le disque.
    /// Prepare() : appel� par BackupService entre la Phase 2 et la Phase 3
    ///             pour pr�parer les dossiers cibles (nettoyage / cr�ation).
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
        /// Analyse les fichiers � sauvegarder et retourne la liste des FileJob.
        /// Op�ration en LECTURE SEULE : ne cr�e, ne modifie ni ne supprime aucun fichier.
        /// Chaque FileJob contient : source, destination, nom du travail, bool�en chiffrement,
        /// bool�en priorit� et taille � permettant le tri en 4 cat�gories en Phase 2.
        /// </summary>
        public abstract List<FileJob> Analyze();

        /// <summary>
        /// Pr�pare les dossiers de destination avant l'ex�cution des copies (Phase 3).
        /// Appel� par BackupService apr�s Task.WaitAll de la Phase 1.
        /// </summary>
        public abstract void Prepare();

        /// <summary>
        /// Valide l'existence du r�pertoire source.
        /// </summary>
        protected void ValidateSource()
        {
            if (!Directory.Exists(SourceDirectory))
                throw new DirectoryNotFoundException(
                    $"Le r�pertoire source '{SourceDirectory}' n'existe pas.");
        }

        /// <summary>
        /// Cr�e un FileJob � partir d'un FileInfo.
        /// D�termine automatiquement IsPriority (extension prioritaire) et IsEncrypted.
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