using Projet_EasySave.EasyLog;
using Projet_EasySave.Models;
using System.Diagnostics;
namespace Projet_EasySave.Services
{
    /// <summary>
    /// Stratégie de sauvegarde complète : copie tous les fichiers.
    /// </summary>
    public class FullBackupStrategy : IBackupStrategy
    {
        public string? ProcessBackup(string source, string target, string Name, JsonLog log)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Le répertoire source n'existe pas : {source}");

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

                    log.WriteLog(new JsonRecord
                    {
                        Timestamp = DateTime.Now,
                        Name = Name,
                        Source = file,
                        Target = destFile,
                        Size = fi.Length,
                        Time = sw.ElapsedMilliseconds,
                        Message = "Success"
                    });
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    log.WriteLog(new JsonRecord
                    {
                        Timestamp = DateTime.Now,
                        Name = Name,
                        Source = file,
                        Target = destFile,
                        Size = fi.Length,
                        Time = -1,
                        Message = ex.Message
                    });
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