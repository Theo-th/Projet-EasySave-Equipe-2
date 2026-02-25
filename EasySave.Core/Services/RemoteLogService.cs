using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Service for sending backup logs to a remote server asynchronously.
    /// </summary>
    public static class RemoteLogService
    {
        private static readonly HttpClient _client = new HttpClient();
        /// <summary>
        /// Sends a backup log record to the remote server asynchronously.
        /// Silently ignores any errors that occur during transmission.
        /// </summary>
        public static async Task SendLogAsync(Record log)
        {
            try
            {
                string serverIp = "localhost";
                /// We check whether a file named ip_server.txt exists in the same directory as the .exe file.
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ip_server.json");

                if (File.Exists(configPath))
                {
                    try {
                        string jsonContent = File.ReadAllText(configPath);

                        // Read the file and remove any spaces or line breaks.

                        var jsonNode = JsonNode.Parse(jsonContent);
                        string? parsedIp = jsonNode?["ServerIp"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(parsedIp))
                        {
                            serverIp = parsedIp.Trim();
                        }
                    }
                    catch { 
                    
                    }
                    
                }
                string url = $"http://{serverIp}:5000/Logs";
                await _client.PostAsJsonAsync(url, log);
            }
            catch (Exception)
            {
            }
        }
    }
}