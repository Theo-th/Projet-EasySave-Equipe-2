using EasySave.Core.Models;
using System.Collections.Generic;

namespace EasySave.Core.Interfaces
{
    /// <summary>
    /// Interface for managing the persistence of the real-time backup state (state.json)
    /// </summary>
    public interface IBackupStateRepository
    {
        /// <summary>
        /// Sets the state file path
        /// </summary>
        /// <param name="path">The state file path</param>
        /// <exception cref="System.ArgumentException">Thrown if the path is null or empty</exception>
        void SetStatePath(string path);

        /// <summary>
        /// Updates the backup job states in the state.json file
        /// </summary>
        /// <param name="jobs">List of backup job states to persist</param>
        void UpdateState(List<BackupJobState> jobs);
    }
}