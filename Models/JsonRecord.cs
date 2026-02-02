using System;
using System.Collections.Generic;
using System.Text;

namespace Projet_EasySave.Models
{
    public class JsonRecord : LogRecord
    // Classe integrant simplement les données
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
