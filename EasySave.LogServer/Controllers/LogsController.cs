using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EasySave.LogServer.Controllers
{
    public class LogEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public long Size { get; set; } = 0;
        public double Time { get; set; } = 0;
        public long EncryptionTime { get; set; } = 0;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("[controller]")]
    public class LogsController : ControllerBase
    {
        private static readonly object _fileLock = new object();
        private readonly string _logDirectory;

        public LogsController(IWebHostEnvironment env)
        {
            _logDirectory = Path.Combine(env.ContentRootPath, "centralized_logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        [HttpPost]
        public IActionResult PostLog([FromBody] LogEntry log)
        {
            try
            {
                string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
                string filePath = Path.Combine(_logDirectory, fileName);

                lock (_fileLock)
                {
                    List<LogEntry> logs = new List<LogEntry>();

                    if (System.IO.File.Exists(filePath))
                    {
                        string existingJson = System.IO.File.ReadAllText(filePath);
                        if (!string.IsNullOrWhiteSpace(existingJson))
                        {
                            try { logs = JsonSerializer.Deserialize<List<LogEntry>>(existingJson) ?? new List<LogEntry>(); }
                            catch { }
                        }
                    }

                    logs.Add(log);

                    string newJson = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(filePath, newJson);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}