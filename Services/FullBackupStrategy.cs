namespace Projet_EasySave.Services
{
    /// <summary>
    /// Stratégie de sauvegarde complète : copie tous les fichiers.
    /// </summary>
    public class FullBackupStrategy : IBackupStrategy
    {
        public void ProcessBackup(string source, string target)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Le répertoire source n'existe pas : {source}");

            Directory.CreateDirectory(target);

            // Copier tous les fichiers
            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(target, fileName);
                File.Copy(file, destFile, true);
            }

            // Copier récursivement les sous-répertoires
            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(target, dirName);
                ProcessBackup(dir, destDir);
            }
        }
    }
}