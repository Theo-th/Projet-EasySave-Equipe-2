using Projet_EasySave.EasyLog;
using Projet_EasySave.Models;
using System.Diagnostics;
namespace Projet_EasySave.Services
{
    /// <summary>
    /// Strat�gie de sauvegarde compl�te : copie tous les fichiers.
    /// </summary>
    public class FullBackupStrategy : IBackupStrategy
    {
        public string? ProcessBackup(string source, string target, string Name, JsonLog log)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Le r�pertoire source n'existe pas : {source}");

            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(target, fileName);

                FileInfo fi = new FileInfo(file);
                Stopwatch sw = new Stopwatch();

                try
                {
                    sw.Start();
                    File.Copy(file, destFile, true);
                    sw.Stop();

                    BackupLogger.LogFileOperation(log, Name, file, destFile, fi.Length, sw.ElapsedMilliseconds, "Success");
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    BackupLogger.LogFileOperation(log, Name, file, destFile, fi.Length, -1, ex.Message);
                }
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(target, dirName);
                ProcessBackup(dir, destDir, Name, log);
            }

            return null;
        }
    }
}