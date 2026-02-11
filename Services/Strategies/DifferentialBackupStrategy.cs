using EasySave.Models;
using EasyLog;
using Projet_EasySave.Models;

namespace Projet_EasySave.Services.Strategies
{
    /// <summary>
    /// Stratégie de sauvegarde différentielle (fichiers modifiés uniquement).
    /// </summary>
    public class DifferentialBackupStrategy : BackupStrategy
    {
        public DifferentialBackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger)
            : base(sourceDirectory, targetDirectory, backupType, jobName, logger)
        {
        }

        /// <summary>
        /// Exécute une sauvegarde différentielle :
        /// 1. Vérifie les dossiers source/destination
        /// 2. Si aucune sauvegarde complète n'existe, en effectue une
        /// 3. Sinon, supprime le dossier différentiel précédent, liste les fichiers modifiés, signale les fichiers supprimés, puis copie
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

                // Étape 2 : Vérifier si une sauvegarde complète existe
                if (!Directory.Exists(fullBackupFolder))
                {
                    return ExecuteFullBackup(fullBackupFolder);
                }

                // Étape 3 : Supprimer le contenu du dossier différentiel précédent
                string diffBackupFolder = Path.Combine(TargetDirectory, DIFFERENTIAL_MARKER);
                ClearBackupFolder(diffBackupFolder);

                var diffFolderCreation = CreateBackupFolder(diffBackupFolder, DIFFERENTIAL_MARKER);
                if (!diffFolderCreation.Success)
                {
                    return diffFolderCreation;
                }

                // 3a : Lister les fichiers modifiés par rapport à la sauvegarde complète
                List<string> modifiedFiles = ListModifiedFilesInSource(fullBackupFolder);

                // 3b : Générer le rapport des fichiers supprimés
                var reportResult = CreateDeletedFilesReport(SourceDirectory, fullBackupFolder, diffBackupFolder);
                if (!reportResult.Success)
                {
                    return reportResult;
                }

                // 3c : Copier les fichiers modifiés depuis la liste
                var copyResult = CopyFilesFromList(modifiedFiles, SourceDirectory, diffBackupFolder);
                if (!copyResult.Success)
                {
                    return copyResult;
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la sauvegarde différentielle : {ex.Message}");
            }
        }

        /// <summary>
        /// Effectue une sauvegarde complète initiale lorsque aucune n'existe.
        /// </summary>
        private (bool Success, string? ErrorMessage) ExecuteFullBackup(string fullBackupFolder)
        {
            var folderCreation = CreateBackupFolder(fullBackupFolder, FULL_MARKER);
            if (!folderCreation.Success)
            {
                return folderCreation;
            }

            List<string> filesToCopy = ListAllFilesInSource();

            var copyResult = CopyFilesFromList(filesToCopy, SourceDirectory, fullBackupFolder);
            if (!copyResult.Success)
            {
                return copyResult;
            }

            return (true, null);
        }

        /// <summary>
        /// Liste tous les fichiers du dossier source sous forme de chemins relatifs.
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

        /// <summary>
        /// Liste les fichiers modifiés dans la source par rapport à la sauvegarde complète.
        /// </summary>
        private List<string> ListModifiedFilesInSource(string fullBackupDir)
        {
            var modifiedFiles = new List<string>();
            var sourceFiles = new DirectoryInfo(SourceDirectory)
                .GetFiles("*", SearchOption.AllDirectories);

            foreach (var sourceFile in sourceFiles)
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, sourceFile.FullName);
                string fullBackupFilePath = Path.Combine(fullBackupDir, relativePath);

                // Fichier nouveau ou modifié
                if (!File.Exists(fullBackupFilePath) ||
                    sourceFile.LastWriteTime > File.GetLastWriteTime(fullBackupFilePath))
                {
                    modifiedFiles.Add(relativePath);
                }
            }

            return modifiedFiles;
        }

        /// <summary>
        /// Détecte les fichiers supprimés (présents dans la sauvegarde complète mais absents de la source)
        /// et génère un rapport dans le dossier de destination.
        /// </summary>
        private (bool Success, string? ErrorMessage) CreateDeletedFilesReport(
            string sourceDir, string fullBackupDir, string targetDir)
        {
            try
            {
                var backupFiles = new DirectoryInfo(fullBackupDir)
                    .GetFiles("*", SearchOption.AllDirectories);

                var deletedFiles = new List<string>();

                foreach (var backupFile in backupFiles)
                {
                    if (IsBackupMarker(backupFile.Name))
                        continue;

                    string relativePath = Path.GetRelativePath(fullBackupDir, backupFile.FullName);
                    string sourceFilePath = Path.Combine(sourceDir, relativePath);

                    if (!File.Exists(sourceFilePath))
                    {
                        deletedFiles.Add(relativePath);
                    }
                }

                if (deletedFiles.Count > 0)
                {
                    string reportPath = Path.Combine(targetDir, DELETED_FILES_REPORT);
                    File.WriteAllLines(reportPath, deletedFiles);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la détection des fichiers supprimés : {ex.Message}");
            }
        }
    }
}