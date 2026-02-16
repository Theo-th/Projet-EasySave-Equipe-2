using EasySave.Core.Models;
using System.Collections.Generic;

namespace EasySave.Core.Interfaces
{
    
    /// <summary>
    /// Interface for managing the persistence of the real-time backup state (state.json).
    /// </summary>
    public interface IBackupStateRepository
    {
        
        /// <summary>
        /// Sets the state file path. Throws ArgumentException if path is null or empty.
        /// </summary>
        /// <param name="path">The path to the state file.</param>
        void SetStatePath(string path);

        
        /// <summary>
        /// Updates the backup job states in the state.json file.
        /// </summary>
        /// <param name="jobs">The list of backup job states.</param>
        void UpdateState(List<BackupJobState> jobs);
    }
}