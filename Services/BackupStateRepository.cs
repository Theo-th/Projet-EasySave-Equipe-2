using ProjetEasySave.Interfaces;
using ProjetEasySave.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetEasySave.Services
{
    /// <summary>
    /// Repository pour la gestion de la persistance de l'état temps réel des sauvegardes (state.json)
    /// </summary>
    public class BackupStateRepository : IBackupStateRepository
    {
        private string _statePath = "./state.json";
        private readonly object _lockObject = new();

        public void SetStatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("State path cannot be null or empty", nameof(path));

            _statePath = path;
        }

        public void UpdateState(List<BackupJob> jobs)
        {
            lock (_lockObject)
            {
                try
                {
                    // Créer le répertoire si nécessaire
                    string? directory = Path.GetDirectoryName(_statePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Options de sérialisation pour un JSON lisible
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new JsonStringEnumConverter() }
                    };

                    // Sérialiser directement la liste de BackupJob
                    string json = JsonSerializer.Serialize(jobs ?? new List<BackupJob>(), options);
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
