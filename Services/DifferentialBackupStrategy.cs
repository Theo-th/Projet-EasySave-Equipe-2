using Projet_EasySave.EasyLog;
using System.Diagnostics;
using Projet_EasySave.Models;
namespace Projet_EasySave.Services
{
    /// <summary>
    /// Strat�gie de sauvegarde diff�rentielle : copie uniquement les fichiers modifi�s
    /// par rapport � la derni�re sauvegarde compl�te.
    /// </summary>
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        private const string FullBackupFolder = "Full";
        private const string DiffBackupFolder = "Diff";
        private readonly Func<string, string, string, JsonLog, string?> _fullBackup;

        public DifferentialBackupStrategy(Func<string, string, string, JsonLog, string?>? fullBackup = null)
        {
            _fullBackup = fullBackup ?? new FullBackupStrategy().ProcessBackup;
        }

        public string? ProcessBackup(string source, string target, string Name, JsonLog log )
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Le r�pertoire source n'existe pas : {source}");

            Directory.CreateDirectory(target);

            string fullBackupPath = Path.Combine(target, FullBackupFolder);
            string diffBackupPath = Path.Combine(target, DiffBackupFolder);

            bool hasFullBackup = Directory.Exists(fullBackupPath) && !IsDirectoryEmpty(fullBackupPath);

            if (!hasFullBackup)
            {
                _fullBackup(source, fullBackupPath, Name, log);
                return "Aucune sauvegarde compl�te trouv�e. Cr�ation de la sauvegarde compl�te de r�f�rence...";
            }

            if (Directory.Exists(diffBackupPath))
                Directory.Delete(diffBackupPath, true);
            Directory.CreateDirectory(diffBackupPath);

            CopyModifiedFiles(source, fullBackupPath, diffBackupPath, Name, log);
            return "Sauvegarde compl�te trouv�e. Cr�ation de la sauvegarde diff�rentielle...";
        }

        private static bool IsDirectoryEmpty(string path) =>
            !Directory.EnumerateFileSystemEntries(path).Any();

        private void CopyModifiedFiles(string source, string fullBackupPath, string diffBackupPath, string Name, JsonLog log)
        {
            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string fullBackupFile = Path.Combine(fullBackupPath, fileName);
                string diffFile = Path.Combine(diffBackupPath, fileName);

                if (!File.Exists(fullBackupFile) || File.GetLastWriteTime(file) > File.GetLastWriteTime(fullBackupFile))
                {
                    string? diffDir = Path.GetDirectoryName(diffFile);
                    if (!string.IsNullOrEmpty(diffDir))
                    {
                        Directory.CreateDirectory(diffDir);
                    }
                    
                    FileInfo fi = new FileInfo(file);
                    Stopwatch sw = new Stopwatch();
                    
                    sw.Start();
                    File.Copy(file, diffFile, true);
                    sw.Stop();

                    BackupLogger.LogFileOperation(log, Name, file, diffFile, fi.Length, sw.ElapsedMilliseconds, "Success (Diff)");
                }
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                string fullSubDir = Path.Combine(fullBackupPath, dirName);
                string diffSubDir = Path.Combine(diffBackupPath, dirName);
                CopyModifiedFiles(dir, fullSubDir, diffSubDir, Name, log);
            }
        }
    }
}