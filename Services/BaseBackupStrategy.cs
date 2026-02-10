using Projet_EasySave.EasyLog;
using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;
using Projet_EasySave.Properties;
using System.Diagnostics;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Abstract base class for backup strategies.
    /// </summary>
    public abstract class BaseBackupStrategy : IBackupStrategy
    {
        public abstract string? ProcessBackup(string source, string target, string name, JsonLog log);

        protected void ValidateSourceDirectory(string source)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException(string.Format(Lang.ExecuteJob, source));
        }

        protected void CopyFileWithLog(string sourceFile, string destFile, string name, JsonLog log, string successMessage = "Success")
        {
            FileInfo fi = new FileInfo(sourceFile);
            Stopwatch sw = new Stopwatch();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                sw.Start();
                File.Copy(sourceFile, destFile, true);
                sw.Stop();

                log.WriteLog(new Record
                {
                    Timestamp = DateTime.Now,
                    Name = name,
                    Source = sourceFile,
                    Target = destFile,
                    Size = fi.Length,
                    Time = sw.ElapsedMilliseconds,
                    Message = successMessage
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                log.WriteLog(new Record
                {
                    Timestamp = DateTime.Now,
                    Name = name,
                    Source = sourceFile,
                    Target = destFile,
                    Size = fi.Length,
                    Time = -1,
                    Message = ex.Message
                });
            }
        }

        protected void ProcessDirectoryRecursively(string source, string target, string name, JsonLog log, Action<string, string, string, JsonLog> fileProcessor)
        {
            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(target, fileName);
                fileProcessor(file, destFile, name, log);
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(target, dirName);
                ProcessDirectoryRecursively(dir, destDir, name, log, fileProcessor);
            }
        }
    }
}