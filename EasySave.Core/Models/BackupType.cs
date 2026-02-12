namespace EasySave.Core.Models
{
    /// <summary>
    /// Backup type
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Full backup (all files)
        /// </summary>
        Complete,

        /// <summary>
        /// Differential backup (modified files only)
        /// </summary>
        Differential
    }
}
