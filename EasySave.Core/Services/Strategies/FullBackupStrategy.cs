using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Stratégie de sauvegarde complète.
    /// Analyse tous les dossiers et fichiers du répertoire source.
    /// </summary>
    public class FullBackupStrategy : BackupStrategy
    {
        public FullBackupStrategy(string sourceDirectory, string targetDirectory, string jobName, HashSet<string> priorityExtensions)
            : base(sourceDirectory, targetDirectory, jobName, priorityExtensions)
        {
        }

        /// <summary>
        /// Analyse tous les fichiers source et retourne la liste des FileJob.
        /// Nettoie le dossier de sauvegarde complète existant avant l'analyse.
        /// </summary>
        public override List<FileJob> Analyze()
        {
            ValidateDirectories();

            string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);

            // Nettoyer et recréer le dossier de sauvegarde complète
            ClearFolder(fullBackupFolder);
            Directory.CreateDirectory(fullBackupFolder);

            // Scanner tous les fichiers source
            var dirInfo = new DirectoryInfo(SourceDirectory);
            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            var fileJobs = new List<FileJob>(allFiles.Length);
            foreach (var file in allFiles)
            {
                fileJobs.Add(CreateFileJob(file, fullBackupFolder));
            }

            return fileJobs;
        }
    }
}