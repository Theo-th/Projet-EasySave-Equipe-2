namespace Projet_EasySave.Services
{
    /// <summary>
    /// Stratégie de sauvegarde différentielle : copie uniquement les fichiers modifiés
    /// par rapport à la dernière sauvegarde complète.
    /// </summary>
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        private const string FullBackupFolder = "Full";
        private const string DiffBackupFolder = "Diff";

        public void ProcessBackup(string source, string target)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Le répertoire source n'existe pas : {source}");

            // Créer le répertoire cible principal
            Directory.CreateDirectory(target);

            string fullBackupPath = Path.Combine(target, FullBackupFolder);
            string diffBackupPath = Path.Combine(target, DiffBackupFolder, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

            // Vérifier si une sauvegarde complète existe
            if (!Directory.Exists(fullBackupPath) || IsDirectoryEmpty(fullBackupPath))
            {
                // Aucune sauvegarde complète : en créer une
                Console.WriteLine("Aucune sauvegarde complète trouvée. Création de la sauvegarde complète de référence...");
                CopyAllFiles(source, fullBackupPath);
            }
            else
            {
                // Sauvegarde complète existante : créer une sauvegarde différentielle
                Console.WriteLine("Sauvegarde complète trouvée. Création de la sauvegarde différentielle...");
                Directory.CreateDirectory(diffBackupPath);
                CopyModifiedFiles(source, fullBackupPath, diffBackupPath);
            }
        }

        /// <summary>
        /// Vérifie si un répertoire est vide.
        /// </summary>
        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// Copie tous les fichiers et sous-répertoires (sauvegarde complète).
        /// </summary>
        private void CopyAllFiles(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(target, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(target, dirName);
                CopyAllFiles(dir, destDir);
            }
        }

        /// <summary>
        /// Copie uniquement les fichiers modifiés par rapport à la sauvegarde complète.
        /// </summary>
        private void CopyModifiedFiles(string source, string fullBackupPath, string diffBackupPath)
        {
            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string fullBackupFile = Path.Combine(fullBackupPath, fileName);
                string diffFile = Path.Combine(diffBackupPath, fileName);

                // Copier si le fichier est nouveau ou modifié par rapport à la sauvegarde complète
                if (!File.Exists(fullBackupFile) || File.GetLastWriteTime(file) > File.GetLastWriteTime(fullBackupFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(diffFile)!);
                    File.Copy(file, diffFile, true);
                }
            }

            // Traiter récursivement les sous-répertoires
            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                string fullSubDir = Path.Combine(fullBackupPath, dirName);
                string diffSubDir = Path.Combine(diffBackupPath, dirName);
                CopyModifiedFiles(dir, fullSubDir, diffSubDir);
            }
        }
    }
}