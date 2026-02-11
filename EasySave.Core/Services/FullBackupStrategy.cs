using Projet_EasySave.EasyLog;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Full backup strategy: copies all files to a "Full" folder.
    /// </summary>
    public class FullBackupStrategy : BaseBackupStrategy
    {
        private const string FullBackupFolder = "Full";

        public override string? ProcessBackup(string source, string target, string name, JsonLog log)
        {
            ValidateSourceDirectory(source);
            Directory.CreateDirectory(target);

            string fullBackupPath = Path.Combine(target, FullBackupFolder);
            Directory.CreateDirectory(fullBackupPath);

            ProcessDirectoryRecursively(source, fullBackupPath, name, log,
                (src, dest, n, l) => CopyFileWithLog(src, dest, n, l));

            return null;
        }
    }
}