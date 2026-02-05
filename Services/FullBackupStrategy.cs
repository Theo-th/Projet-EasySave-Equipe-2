namespace Projet_EasySave.Services
{
    /// <summary>
    /// Stratégie de sauvegarde complète : copie tous les fichiers.
    /// </summary>
    public class FullBackupStrategy : IBackupStrategy
    {
        public string? ProcessBackup(string source, string target)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Le répertoire source n'existe pas : {source}");

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
                ProcessBackup(dir, destDir);
            }

            return null;
        }
    }
}