using System;
using System.Collections.Generic;
using System.Text;

namespace Projet_EasySave.EasyLog.Models
{
    /// <summary>
    /// Represents a log record for JSON format.
    /// </summary>
    public class JsonRecord : LogRecord
    {
        public DateTime Timestamp { get; set; }

        public string Message { get; set; }

        public string Name { get; set; }

        public string Source { get; set; }

        public string Target { get; set; }

        public long Size { get; set; }

        public long Time { get; set; }
    }
}
