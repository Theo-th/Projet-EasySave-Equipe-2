using EasySave.Core.Models;
using System.Collections.Generic;

namespace EasySave.Core.Interfaces
{
    
    // Interface for managing the persistence of the real-time backup state (state.json)
    public interface IBackupStateRepository
    {
        
        // Sets the state file path. Throws ArgumentException if path is null or empty.
        void SetStatePath(string path);

        
        // Updates the backup job states in the state.json file.
        void UpdateState(List<BackupJobState> jobs);
    }
}