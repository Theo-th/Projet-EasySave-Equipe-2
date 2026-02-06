using System;
using System.Collections.Generic;
using System.Text;

namespace Projet_EasyLog.Models
{
    /// <summary>
    /// Modèle de données représentant un enregistrement de log spécifique au format JSON.
    /// Cette classe contient toutes les informations d'une sauvegarde.
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
