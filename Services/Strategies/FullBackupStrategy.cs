using EasySave.Models;
using EasyLog;
using Projet_EasySave.Models;

namespace Projet_EasySave.Services.Strategies
{
    /// <summary>
    /// Stratégie de sauvegarde complète.
    /// </summary>
    public class FullBackupStrategy : BackupStrategy
    {
        public FullBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger)
            : base(sourceDirectory, targetDirectory, backupType, jobName, logger)
        {
        }

        /// <summary>
        /// Exécute une sauvegarde complète :
        /// 1. Vérifie les dossiers source/destination
        /// 2. Liste tous les fichiers à copier
        /// 3. Copie les fichiers depuis la liste
        /// </summary>
        public override (bool Success, string? ErrorMessage) Execute()
        {
            // Étape 1 : Validation des dossiers
            var validation = ValidateAndPrepareDirectories();
            if (!validation.Success)
            {
                return validation;
            }

            try
            {
                string fullBackupFolder = Path.Combine(TargetDirectory, FULL_MARKER);
                
                // Nettoyer le contenu précédent s'il existe
                ClearBackupFolder(fullBackupFolder);

                var folderCreation = CreateBackupFolder(fullBackupFolder, FULL_MARKER);
                if (!folderCreation.Success)
                {
                    return folderCreation;
                }

                // Étape 2 : Lister tous les fichiers à copier
                List<string> filesToCopy = ListAllFilesInSource();

                // Calculer la taille totale et notifier l'initialisation
                long totalSize = ComputeTotalSize(filesToCopy, SourceDirectory);
                RaiseBackupInitialized(filesToCopy.Count, totalSize);

                // Étape 3 : Copier les fichiers depuis la liste
                var copyResult = CopyFilesFromList(filesToCopy, SourceDirectory, fullBackupFolder);
                if (!copyResult.Success)
                {
                    return copyResult;
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la sauvegarde complète : {ex.Message}");
            }
        }

        /// <summary>
        /// Liste récursivement tous les fichiers du dossier source sous forme de chemins relatifs.
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