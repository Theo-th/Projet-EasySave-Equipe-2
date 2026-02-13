using EasySave.Core.Models;
using EasyLog;
using System.Diagnostics;

namespace EasySave.Core.Services.Strategies
{
    // Abstract class defining the backup strategy.
    public abstract class BackupStrategy
    {
        protected const string FULL_MARKER = "full";
        protected const string DIFFERENTIAL_MARKER = "differential";
        protected const string DELETED_FILES_REPORT = "_deleted_files.txt";

        protected string SourceDirectory { get; set; }
        protected string TargetDirectory { get; set; }
        protected BackupType BackupType { get; set; }
        protected string JobName { get; set; }
        protected BaseLog Logger { get; set; }
        
        /// <summary>
        /// Flag pour arr�ter la sauvegarde en cours.
        /// </summary>
        protected bool _shouldStop = false;

        // Event triggered before file copy, with the total file count and total size.
        public event Action<int, long>? OnBackupInitialized;

        // Event triggered after each file transfer (sourceFile, targetFile, fileSize).
        public event Action<string, string, long>? OnFileTransferred;

        public BackupStrategy(string sourceDirectory, string targetDirectory, BackupType backupType, string jobName, BaseLog logger)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            BackupType = backupType;
            JobName = jobName;
            Logger = logger;
        }

        // Executes the backup strategy.
        public abstract (bool Success, string? ErrorMessage) Execute();

        /// <summary>
        /// Arr�te la sauvegarde en cours de mani�re gracieuse.
        /// </summary>
        public void Stop()
        {
            _shouldStop = true;
        }

        /// <summary>
        /// Valide et pr�pare les r�pertoires source et destination.
        /// </summary>
        protected (bool Success, string? ErrorMessage) ValidateAndPrepareDirectories()
        {
            if (!Directory.Exists(SourceDirectory))
            {
                return (false, $"Source directory '{SourceDirectory}' does not exist.");
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
                return (false, $"Unable to create the destination directory: {ex.Message}");
            }
        }

        /// <summary>
        /// V�rifie si un fichier est un marqueur de sauvegarde.
        /// </summary>
        protected bool IsBackupMarker(string fileName)
        {
            return fileName == FULL_MARKER || fileName == DIFFERENTIAL_MARKER;
        }

        /// <summary>
        /// Cr�e un dossier de sauvegarde et son fichier marqueur.
        /// </summary>
        protected (bool Success, string? ErrorMessage) CreateBackupFolder(string backupFolderPath, string markerFileName)
        {
            try
            {
                Directory.CreateDirectory(backupFolderPath);

                string markerFilePath = Path.Combine(backupFolderPath, markerFileName);
                string markerContent = $"Backup {markerFileName} created on {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

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
                return (false, $"Error creating the backup folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Calcule la taille totale d'une liste de fichiers relative � un r�pertoire source.
        /// </summary>
        protected long ComputeTotalSize(List<string> relativeFilePaths, string sourceDir)
        {
            long totalSize = 0;
            foreach (string relativePath in relativeFilePaths)
            {
                var fileInfo = new FileInfo(Path.Combine(sourceDir, relativePath));
                if (fileInfo.Exists)
                    totalSize += fileInfo.Length;
            }
            return totalSize;
        }

        /// <summary>
        /// D�clenche l'�v�nement d'initialisation de sauvegarde.
        /// </summary>
        protected void RaiseBackupInitialized(int totalFiles, long totalSize)
        {
            OnBackupInitialized?.Invoke(totalFiles, totalSize);
        }

        /// <summary>
        /// D�clenche l'�v�nement de transfert de fichier.
        /// </summary>
        protected void RaiseFileTransferred(string sourceFile, string targetFile, long fileSize)
        {
            OnFileTransferred?.Invoke(sourceFile, targetFile, fileSize);
        }

        /// <summary>
        /// Copie une liste de fichiers (chemins relatifs) d'un r�pertoire source vers un r�pertoire cible.
        /// Chaque transfert est enregistr� et d�clenche un �v�nement de progression.
        /// </summary>
        protected (bool Success, string? ErrorMessage) CopyFile(
            string relativePath, string sourceDir, string targetDir)
        {
            try
            {
                string sourceFilePath = Path.Combine(sourceDir, relativePath);
                string targetFilePath = Path.Combine(targetDir, relativePath);

                // Create necessary subdirectories
                string? targetFileDir = Path.GetDirectoryName(targetFilePath);
                if (targetFileDir != null && !Directory.Exists(targetFileDir))
                {
                    // V�rifier si l'arr�t a �t� demand�
                    if (_shouldStop)
                    {
                        return (false, "Backup stopped: watched process detected.");
                    }

                    string sourceFilePath = Path.Combine(sourceDir, relativePath);
                    string targetFilePath = Path.Combine(targetDir, relativePath);

                var fileInfo = new FileInfo(sourceFilePath);
                long fileSize = fileInfo.Length;

                var stopwatch = Stopwatch.StartNew();
                File.Copy(sourceFilePath, targetFilePath, overwrite: true);
                stopwatch.Stop();

                long encryptionTime = EncryptionService.Instance.EncryptFile(targetFilePath);

                var record = new Record
                {
                    Name = JobName,
                    Source = sourceFilePath,
                    Target = targetFilePath,
                    Size = fileSize,
                    Time = stopwatch.Elapsed.TotalMilliseconds,
                    Timestamp = DateTime.Now,
                    EncryptionTime = encryptionTime,
                };

                Logger.WriteLog(record);

                // Notify progress after file copied
                RaiseFileTransferred(sourceFilePath, targetFilePath, fileSize);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error copying file '{relativePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Vide le contenu d'un dossier de sauvegarde sans supprimer le dossier lui-m�me.
        /// </summary>
        protected void ClearBackupFolder(string backupFolder)
        {
            try
            {
                if (Directory.Exists(backupFolder))
                {
                    var dirInfo = new DirectoryInfo(backupFolder);
                    
                    // Delete all files
                    foreach (var file in dirInfo.GetFiles())
                    {
                        file.Delete();
                    }
                    
                    // Delete all subdirectories
                    foreach (var subDir in dirInfo.GetDirectories())
                    {
                        subDir.Delete(recursive: true);
                    }
                }
                else
                {
                    // If the folder does not exist, create it
                    Directory.CreateDirectory(backupFolder);
                }
            }
            catch (Exception)
            {
                // On error, ensure the folder exists
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }
            }
        }
    }
}