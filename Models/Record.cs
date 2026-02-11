using System;

namespace EasySave.Models
{
    public class Record
    {
        public required string Name { get; set; }
        public required string Source { get; set; }
        public required string Target { get; set; }
        public required long Size { get; set; }
        public required double Time { get; set; }
        public required DateTime Timestamp { get; set; }
    }
}