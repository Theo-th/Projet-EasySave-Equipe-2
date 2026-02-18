using System;
using System.IO;
using System.Text.Json;

namespace EasySave.Core.Services
{
    public class NetworkConfig
    {
        public string ServerIp { get; set; } = "localhost";
    }

    public class NetworkService
    {
        private static NetworkService _instance = new NetworkService();
        public static NetworkService Instance => _instance;

        private NetworkConfig _config = new NetworkConfig();
        private readonly string _configFilePath;

        private NetworkService()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "network_config.json");
            LoadConfig();
        }

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

        public void SetServerIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            _config.ServerIp = ip.Trim();
            SaveConfig();
        }

        public string GetServerIp() => _config.ServerIp;

        public string GetServerUrl()
        {
            return $"http://{_config.ServerIp}:5000/Logs";
        }
    }
}