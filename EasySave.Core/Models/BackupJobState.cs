using EasySave.Core.Models;
using System;

namespace EasySave.Core.Models
{
    // Represents the state of a backup job for real-time persistence
        public class BackupJobState
        {
            // Timestamp du début de la sauvegarde
            public DateTime StartTimestamp { get; set; } = DateTime.Now;
        // Unique job identifier (1 to 5)
        public int Id { get; set; }

        // Backup job name
        public string Name { get; set; } = string.Empty;

        // Source directory path
        public string SourcePath { get; set; } = string.Empty;

        // Target directory path
        public string TargetPath { get; set; } = string.Empty;

        // Backup type (Full or Differential)
        public BackupType Type { get; set; } = BackupType.Complete;

        // Current job state
        public BackupState State { get; set; } = BackupState.Inactive;

        // Last action timestamp
        public DateTime LastActionTimestamp { get; set; } = DateTime.Now;

        // Total number of files to back up
        public int TotalFiles { get; set; }

        // Total size of files to back up (in bytes)
        public long TotalSize { get; set; }

        // Number of remaining files to back up
        public int RemainingFiles { get; set; }

        // Remaining size to back up (in bytes)
        public long RemainingSize { get; set; }

        // Source file currently being backed up
        public string CurrentSourceFile { get; set; } = string.Empty;

        // Target file currently being backed up
        public string CurrentTargetFile { get; set; } = string.Empty;

        // Progress percentage (0-100)
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
