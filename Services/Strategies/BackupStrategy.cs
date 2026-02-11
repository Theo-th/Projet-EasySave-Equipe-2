using EasySave.Models;
using EasyLog;
using Projet_EasySave.Models;
using System.Diagnostics;

namespace Projet_EasySave.Services.Strategies
{
    /// <summary>
    /// Classe abstraite définissant la stratégie de sauvegarde.
    /// </summary>
    public abstract class BackupStrategy
    {
        // Constantes pour les fichiers marqueurs
        protected const string FULL_MARKER = "full";
        protected const string DIFFERENTIAL_MARKER = "differential";
        protected const string DELETED_FILES_REPORT = "_deleted_files.txt";

        protected string SourceDirectory { get; set; }
        protected string TargetDirectory { get; set; }
        protected BackupType BackupType { get; set; }
        protected string JobName { get; set; }
        protected BaseLog Logger { get; set; }

        public BackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            BackupType = backupType;
            JobName = jobName;
            Logger = logger;
        }

        /// <summary>
        /// Exécute la stratégie de sauvegarde.
        /// </summary>
        public abstract (bool Success, string? ErrorMessage) Execute();

        /// <summary>
        /// Valide et prépare les dossiers source et destination.
        /// </summary>
        protected (bool Success, string? ErrorMessage) ValidateAndPrepareDirectories()
        {
            if (!Directory.Exists(SourceDirectory))
            {
                return (false, $"Le dossier source '{SourceDirectory}' n'existe pas.");
            }

            try
            {
                if (!Directory.Exists(TargetDirectory))
                {
                    Directory.CreateDirectory(TargetDirectory);
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Impossible de créer le dossier de destination : {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si un fichier est un marqueur de sauvegarde.
        /// </summary>
        protected bool IsBackupMarker(string fileName)
        {
            return fileName == FULL_MARKER || fileName == DIFFERENTIAL_MARKER;
        }

        /// <summary>
        /// Crée un dossier de sauvegarde et son fichier marqueur.
        /// </summary>
        protected (bool Success, string? ErrorMessage) CreateBackupFolder(string backupFolderPath, string markerFileName)
        {
            try
            {
                Directory.CreateDirectory(backupFolderPath);

                string markerFilePath = Path.Combine(backupFolderPath, markerFileName);
                string markerContent = $"Sauvegarde {markerFileName} créée le {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                var stopwatch = Stopwatch.StartNew();
                File.WriteAllText(markerFilePath, markerContent);
                stopwatch.Stop();

                var record = new Record
                {
                    Name = JobName,
                    Source = "",
                    Target = markerFilePath,
                    Size = markerContent.Length,
                    Time = stopwatch.Elapsed.TotalMilliseconds,
                    Timestamp = DateTime.Now
                };

                Logger.WriteLog(record);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création du dossier de sauvegarde : {ex.Message}");
            }
        }

        /// <summary>
        /// Copie une liste de fichiers (chemins relatifs) depuis un dossier source vers un dossier cible.
        /// Chaque transfert est logué via Logger.WriteLog().
        /// </summary>
        protected (bool Success, string? ErrorMessage) CopyFilesFromList(
            List<string> relativeFilePaths, string sourceDir, string targetDir)
        {
            try
            {
                foreach (string relativePath in relativeFilePaths)
                {
                    string sourceFilePath = Path.Combine(sourceDir, relativePath);
                    string targetFilePath = Path.Combine(targetDir, relativePath);

                    // Créer les sous-dossiers nécessaires
                    string? targetFileDir = Path.GetDirectoryName(targetFilePath);
                    if (targetFileDir != null && !Directory.Exists(targetFileDir))
                    {
                        Directory.CreateDirectory(targetFileDir);
                    }

                    var fileInfo = new FileInfo(sourceFilePath);
                    long fileSize = fileInfo.Length;

                    var stopwatch = Stopwatch.StartNew();
                    File.Copy(sourceFilePath, targetFilePath, overwrite: true);
                    stopwatch.Stop();

                    var record = new Record
                    {
                        Name = JobName,
                        Source = sourceFilePath,
                        Target = targetFilePath,
                        Size = fileSize,
                        Time = stopwatch.Elapsed.TotalMilliseconds,
                        Timestamp = DateTime.Now
                    };

                    Logger.WriteLog(record);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la copie des fichiers : {ex.Message}");
            }
        }

        /// <summary>
        /// Supprime le contenu d'un dossier de sauvegarde sans supprimer le dossier lui-même.
        /// </summary>
        protected void ClearBackupFolder(string backupFolder)
        {
            try
            {
                if (Directory.Exists(backupFolder))
                {
                    var dirInfo = new DirectoryInfo(backupFolder);
                    
                    // Supprimer tous les fichiers
                    foreach (var file in dirInfo.GetFiles())
                    {
                        file.Delete();
                    }
                    
                    // Supprimer tous les sous-dossiers
                    foreach (var subDir in dirInfo.GetDirectories())
                    {
                        subDir.Delete(recursive: true);
                    }
                }
                else
                {
                    // Si le dossier n'existe pas, le créer
                    Directory.CreateDirectory(backupFolder);
                }
            }
            catch (Exception)
            {
                // En cas d'erreur, s'assurer que le dossier existe
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }
            }
        }
    }
}