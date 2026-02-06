using Projet_EasySave.EasyLog;
using Projet_EasySave.Properties;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Differential backup strategy: copies only modified files.
    /// </summary>
    public class DifferentialBackupStrategy : BaseBackupStrategy
    {
        private const string FullBackupFolder = "Full";
        private const string DiffBackupFolder = "Diff";

        public override string? ProcessBackup(string source, string target, string name, JsonLog log)
        {
            ValidateSourceDirectory(source);
            Directory.CreateDirectory(target);

            string fullBackupPath = Path.Combine(target, FullBackupFolder);
            string diffBackupPath = Path.Combine(target, DiffBackupFolder);

            if (!Directory.Exists(fullBackupPath) || IsDirectoryEmpty(fullBackupPath))
            {
                new FullBackupStrategy().ProcessBackup(source, fullBackupPath, name, log);
                return Lang.NoFullBackupFound;
            }

            if (Directory.Exists(diffBackupPath))
                Directory.Delete(diffBackupPath, true);
            Directory.CreateDirectory(diffBackupPath);

            CopyModifiedFiles(source, fullBackupPath, diffBackupPath, name, log);
            return Lang.DifferentialBackupCreated;
        }

        private static bool IsDirectoryEmpty(string path) =>
            !Directory.EnumerateFileSystemEntries(path).Any();

        private void CopyModifiedFiles(string source, string fullBackupPath, string diffBackupPath, string name, JsonLog log)
        {
            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string fullBackupFile = Path.Combine(fullBackupPath, fileName);
                string diffFile = Path.Combine(diffBackupPath, fileName);

                if (!File.Exists(fullBackupFile) || File.GetLastWriteTime(file) > File.GetLastWriteTime(fullBackupFile))
                {
                    CopyFileWithLog(file, diffFile, name, log, "Success (Diff)");
                }
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                CopyModifiedFiles(dir, Path.Combine(fullBackupPath, dirName), Path.Combine(diffBackupPath, dirName), name, log);
            }
        }
    }
}