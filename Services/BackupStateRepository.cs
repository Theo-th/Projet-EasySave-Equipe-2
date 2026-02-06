using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Repository for managing real-time backup state persistence (state.json).
    /// </summary>
    public class BackupStateRepository : IBackupStateRepository
    {
        private string _statePath;
        private readonly object _lockObject = new();

        private static readonly JsonSerializerOptions StateOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public BackupStateRepository()
        {
            _statePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");
        }

        public void SetStatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("State path cannot be null or empty", nameof(path));

            _statePath = path;
        }

        public void UpdateState(List<BackupJobState> jobs)
        {
            lock (_lockObject)
            {
                try
                {
                    string? directory = Path.GetDirectoryName(_statePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string json = JsonSerializer.Serialize(jobs ?? new List<BackupJobState>(), StateOptions);
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
