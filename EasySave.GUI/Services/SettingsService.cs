using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.GUI.Services;

/// <summary>
/// Service for managing application settings persistence.
/// Handles loading and saving settings to a JSON file.
/// </summary>
public class SettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsFilePath;

    /// <summary>
    /// Initializes a new instance of SettingsService and sets the settings file path.
    /// </summary>
    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
    }

    /// <summary>
    /// Loads application settings from the JSON file.
    /// </summary>
    /// <returns>Dictionary of settings key-value pairs.</returns>
    public Dictionary<string, string> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                    ?? new Dictionary<string, string>();
            }
        }
        catch
        {
            // If an error occurs, return an empty dictionary
        }
        return new Dictionary<string, string>();
    }

    /// <summary>
    /// Saves application settings to the JSON file.
    /// </summary>
    /// <param name="logsPath">Path to logs directory.</param>
    /// <param name="configPath">Path to config file.</param>
    /// <param name="statePath">Path to state file.</param>
    /// <returns>True if save succeeded, false otherwise.</returns>
    public bool SaveSettings(string logsPath, string configPath, string statePath)
    {
        try
        {
            var settings = new Dictionary<string, string>
            {
                ["LogsPath"] = logsPath,
                ["ConfigPath"] = configPath,
                ["StatePath"] = statePath
            };
            
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
