using Projet_EasySave.Models;
using System;

namespace Projet_EasySave.Models
{
    /// <summary>
    /// Represents the state of a backup job for real-time persistence
    /// </summary>
    public class BackupJobState
    {
        /// <summary>
        /// Unique job identifier (1 to 5)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Backup job name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Source directory path
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Target directory path
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// Backup type (Full or Differential)
        /// </summary>
        public BackupType Type { get; set; } = BackupType.Complete;

        /// <summary>
        /// Current job state
        /// </summary>
        public BackupState State { get; set; } = BackupState.Inactive;

        /// <summary>
        /// Last action timestamp
        /// </summary>
        public DateTime LastActionTimestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Total number of files to back up
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Total size of files to back up (in bytes)
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Number of remaining files to back up
        /// </summary>
        public int RemainingFiles { get; set; }

        /// <summary>
        /// Remaining size to back up (in bytes)
        /// </summary>
        public long RemainingSize { get; set; }

        /// <summary>
        /// Source file currently being backed up
        /// </summary>
        public string CurrentSourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Target file currently being backed up
        /// </summary>
        public string CurrentTargetFile { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage
        {
            get
            {
                if (TotalSize == 0) return 0;
                return (int)((TotalSize - RemainingSize) * 100.0 / TotalSize);
            }
        }
    }
}
