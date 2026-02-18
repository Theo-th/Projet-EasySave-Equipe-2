namespace EasySave.Core.Models
{
    public class Record
    {
        public required string Name { get; set; }
        public required string Source { get; set; }
        public required string Target { get; set; }
        public long Size { get; set; }

        // Transfer time (copy)
        public double Time { get; set; }

        // Encryption time in ms (0 if not encrypted, -1 if error)
        public long EncryptionTime { get; set; }

        public DateTime Timestamp { get; set; }

        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;       
    }
}