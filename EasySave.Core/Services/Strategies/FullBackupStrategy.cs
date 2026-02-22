using EasySave.Core.Models;
using EasyLog;

namespace EasySave.Core.Services.Strategies
{
    /// <summary>
    /// Stratégie de sauvegarde complète.
    /// Analyze() : scan de tous les fichiers source (lecture seule).
    /// Prepare() : nettoie et recrée le dossier 'full' avant les copies.
    /// </summary>
    public class FullBackupStrategy : BackupStrategy
    {
        public FullBackupStrategy(string sourceDirectory, string targetDirectory,
            string jobName, HashSet<string> priorityExtensions)
            : base(sourceDirectory, targetDirectory, jobName, priorityExtensions)
        {
        }

        /// <summary>
        /// Scanne tous les fichiers source et retourne les FileJob avec destination dans 'full/'.
        /// Ne touche pas au disque : aucune création ni suppression de fichiers/dossiers.
        /// </summary>
        public override List<FileJob> Analyze()
        {
            ValidateSource();

            string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);

            var dirInfo = new DirectoryInfo(SourceDirectory);
            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

            var fileJobs = new List<FileJob>(allFiles.Length);
            foreach (var file in allFiles)
                fileJobs.Add(CreateFileJob(file, fullBackupFolder));

            return fileJobs;
        }

        /// <summary>
        /// Nettoie et recrée le dossier de sauvegarde complète.
        /// Appelé par BackupService juste avant la Phase 3.
        /// </summary>
        public override void Prepare()
        {
            string fullBackupFolder = Path.Combine(TargetDirectory, FULL_FOLDER);
            ClearFolder(fullBackupFolder);
            Directory.CreateDirectory(fullBackupFolder);
        }
    }
}