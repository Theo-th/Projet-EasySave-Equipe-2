namespace EasySave.Core.Models
{
    // Defines the type of backup operation.
    public enum BackupType
    {
        // Full backup (all files)
        Complete,

        // Differential backup (modified files only)
        Differential
    }
}
