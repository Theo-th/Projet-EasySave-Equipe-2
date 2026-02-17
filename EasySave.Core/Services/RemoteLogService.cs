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

        public static async Task SendLogAsync(Record log)
        {
            try
            {
                string dynamicUrl = NetworkService.Instance.GetServerUrl();
                using var cts = new System.Threading.CancellationTokenSource(2000);
                await _client.PostAsJsonAsync(dynamicUrl, log, cts.Token);
            }
            catch (Exception)
            {
            }
        }
    }
}