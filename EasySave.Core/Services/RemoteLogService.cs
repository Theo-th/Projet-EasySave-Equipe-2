using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    public static class RemoteLogService
    {
        private static readonly HttpClient _client = new HttpClient();
        private const string ServerUrl = "http://localhost:5000/Logs";

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