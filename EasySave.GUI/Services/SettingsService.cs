using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.GUI.Services;

// Service for managing application settings persistence
public class SettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
    }

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
