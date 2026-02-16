using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Core.Services
{
    
    /// <summary>
    /// Repository for managing the persistence of the real-time backup state (state.json).
    /// </summary>
    public class BackupStateRepository : IBackupStateRepository
    {
        private string _statePath = "./state.json";
        private readonly object _lockObject = new();

        /// <summary>
        /// Sets the path to the state file.
        /// </summary>
        /// <param name="path">The path to the state file.</param>
        public void SetStatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("State path cannot be null or empty", nameof(path));

            _statePath = path;
        }

        /// <summary>
        /// Updates the state file with the current list of backup jobs.
        /// </summary>
        /// <param name="jobs">The list of backup job states.</param>
        public void UpdateState(List<BackupJobState> jobs)
        {
            lock (_lockObject)
            {
                try
                {
                    // Create the directory if necessary
                    string? directory = Path.GetDirectoryName(_statePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Serialization options for readable JSON
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new JsonStringEnumConverter() }
                    };

                    // Serialize the BackupJobState list directly
                    string json = JsonSerializer.Serialize(jobs ?? new List<BackupJobState>(), options);
                    File.WriteAllText(_statePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating state file: {ex.Message}");
                }
            }
        }
    }
}
