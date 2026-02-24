using System;
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
        private const string ServerUrl = "http://localhost:5000/Logs";

        /// <summary>
        /// Sends a backup log record to the remote server asynchronously.
        /// Silently ignores any errors that occur during transmission.
        /// </summary>
        public static async Task SendLogAsync(Record log)
        {
            try
            {
                await _client.PostAsJsonAsync(ServerUrl, log);
            }
            catch (Exception)
            {
            }
        }
    }
}