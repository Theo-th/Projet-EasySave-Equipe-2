using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Configuration data structure for encryption settings (persisted in JSON).
    /// </summary>
    public class EncryptionConfig
    {
        /// <summary>
        /// Gets or sets the encryption key.
        /// </summary>
        public string Key { get; set; } = "DefaultKey";

        /// <summary>
        /// Gets or sets the list of file extensions to be encrypted.
        /// </summary>
        public List<string> Extensions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Service responsible for managing file encryption using the external CryptoSoft software.
    /// </summary>
    public class EncryptionService
    {
        private static readonly EncryptionService _instance = new EncryptionService();

        /// <summary>
        /// Gets the singleton instance of the EncryptionService.
        /// </summary>
        public static EncryptionService Instance => _instance;

        private EncryptionConfig _config = new EncryptionConfig();
        private readonly string _configFilePath;
        private readonly string _cryptoSoftPath;

        // LOCK ADDED HERE: Static object acting as a queue for the external process
        private static readonly object _cryptoLock = new object();

        /// <summary>
        /// Initializes a new instance of the EncryptionService class.
        /// Sets up paths for configuration and the CryptoSoft executable.
        /// </summary>
        private EncryptionService()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configFilePath = Path.Combine(baseDir, "cryptosoft_config.json");

            // Path to the external tool in the "Tools" subdirectory
            _cryptoSoftPath = Path.Combine(baseDir, "Tools", "CryptoSoft.exe");

            LoadConfig();
        }


        /// <summary>
        /// Loads the configuration from the JSON file.
        /// Creates a default configuration if the file does not exist.
        /// </summary>
        private void LoadConfig()
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_configFilePath);
                    _config = JsonSerializer.Deserialize<EncryptionConfig>(json) ?? new EncryptionConfig();
                }
                catch
                {
                    _config = new EncryptionConfig();
                }
            }
            else
            {
                _config = new EncryptionConfig();
                SaveConfig();
            }
        }

        /// <summary>
        /// Saves the current configuration to the JSON file.
        /// </summary>
        private void SaveConfig()
        {

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }


        /// <summary>
        /// Sets the encryption key and saves the configuration.
        /// </summary>
        /// <param name="key">The new encryption key.</param>
        public void SetKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            _config.Key = key;
            SaveConfig();
        }

        /// <summary>
        /// Retrieves the current encryption key.
        /// </summary>
        /// <returns>The encryption key.</returns>
        public string GetKey() => _config.Key;

        /// <summary>
        /// Adds a file extension to the encryption list.
        /// </summary>
        /// <param name="extension">The extension to add (e.g., ".txt").</param>
        public void AddExtension(string extension)
        {
            string fmtExt = FormatExtension(extension);
            if (!_config.Extensions.Contains(fmtExt))
            {
                _config.Extensions.Add(fmtExt);
                SaveConfig();
            }
        }

        /// <summary>
        /// Removes a file extension from the encryption list.
        /// </summary>
        /// <param name="extension">The extension to remove.</param>
        public void RemoveExtension(string extension)
        {
            string fmtExt = FormatExtension(extension);
            if (_config.Extensions.Contains(fmtExt))
            {
                _config.Extensions.Remove(fmtExt);
                SaveConfig();
            }
        }

        /// <summary>
        /// Retrieves the list of configured extensions.
        /// </summary>
        /// <returns>A copy of the extension list to prevent side effects.</returns>
        public List<string> GetExtensions()
        {
            return new List<string>(_config.Extensions);
        }

        /// <summary>
        /// Formats the extension string (trims, lowers case, adds dot if missing).
        /// </summary>
        /// <param name="extension">The raw extension string.</param>
        /// <returns>The formatted extension string.</returns>
        private string FormatExtension(string extension)
        {
            string ext = extension.Trim().ToLower();
            return ext.StartsWith(".") ? ext : "." + ext;
        }


        /// <summary>
        /// Encrypts the specified file if its extension is configured.
        /// </summary>
        /// <param name="filePath">The path of the file to encrypt.</param>
        /// <returns>
        /// 0 if the file was not encrypted (extension not in list).
        /// The encryption time in milliseconds if successful.
        /// -1 if an error occurred (e.g., CryptoSoft missing).
        /// </returns>
        public long EncryptFile(string filePath)
        {
            if (!File.Exists(filePath)) return -1;

            string fileExtension = Path.GetExtension(filePath).ToLower();

            if (!_config.Extensions.Contains(fileExtension))
            {
                return 0; // Not targeted
            }

            if (!File.Exists(_cryptoSoftPath))
            {
                return -1; // Executable missing
            }

            try
            {
                // QUEUEING HERE: only one thread allowed to pass at a time
                lock (_cryptoLock)
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = _cryptoSoftPath,

                        Arguments = $"\"{filePath}\" \"{_config.Key}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processStartInfo))
                    {
                        if (process == null) return -1;


                        process.WaitForExit();

                        // CryptoSoft returns the time (in ms) via the exit code
                        return process.ExitCode;
                    }
                }
            }
            catch
            {
                return -1;
            }
        }
    }
}