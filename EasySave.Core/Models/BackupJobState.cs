using System;

namespace EasySave.Core.Models
{
    /// <summary>
    /// Represents the state of a backup job for real-time persistence.
    /// </summary>
    public class BackupJobState
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        public string TargetPath { get; set; } = string.Empty;

        public BackupType Type { get; set; } = BackupType.Complete;

        public BackupState State { get; set; } = BackupState.Inactive;

        public DateTime LastActionTimestamp { get; set; } = DateTime.Now;

        public int TotalFiles { get; set; }

        public long TotalSize { get; set; }

        public int RemainingFiles { get; set; }

        public long RemainingSize { get; set; }

        public string CurrentSourceFile { get; set; } = string.Empty;

        public string CurrentTargetFile { get; set; } = string.Empty;

        public int ProgressPercentage
        {
            get
            {
                if (TotalSize == 0) return 0;
                long processedSize = TotalSize - RemainingSize;
                return (int)(processedSize * 100.0 / TotalSize);
            }
        }
        
        public int ProgressPercentageByFiles
        {
            get
            {
                if (TotalFiles == 0) return 0;
                return (int)((TotalFiles - RemainingFiles) * 100.0 / TotalFiles);
            }
        }
    }
}
