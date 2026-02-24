using System;
using System.IO;
using System.Text.Json;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Configuration model for network settings.
    /// </summary>
    public class NetworkConfig
    {
        public string ServerIp { get; set; } = "localhost";
    }

    /// <summary>
    /// Singleton service for managing network configuration.
    /// Handles server IP configuration and persistence.
    /// </summary>
    public class NetworkService
    {
        private static NetworkService _instance = new NetworkService();
        public static NetworkService Instance => _instance;

        private NetworkConfig _config = new NetworkConfig();
        private readonly string _configFilePath;

        /// <summary>
        /// Private constructor for singleton pattern initialization.
        /// Loads network configuration from file.
        /// </summary>
        private NetworkService()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "network_config.json");
            LoadConfig();
        }

        /// <summary>
        /// Loads network configuration from the configuration file.
        /// Uses default configuration if file does not exist or is invalid.
        /// </summary>
        private void LoadConfig()
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_configFilePath);
                    _config = JsonSerializer.Deserialize<NetworkConfig>(json) ?? new NetworkConfig();
                }
                catch { _config = new NetworkConfig(); }
            }
            else
            {
                SaveConfig();
            }
        }

        /// <summary>
        /// Saves the current network configuration to file.
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch { }
        }

        /// <summary>
        /// Sets the server IP address and persists the configuration.
        /// </summary>
        public void SetServerIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            _config.ServerIp = ip.Trim();
            SaveConfig();
        }

        /// <summary>
        /// Returns the currently configured server IP address.
        /// </summary>
        public string GetServerIp() => _config.ServerIp;

        /// <summary>
        /// Returns the full server URL for logging endpoint.
        /// </summary>
        public string GetServerUrl()
        {
            return $"http://{_config.ServerIp}:5000/Logs";
        }
    }
}