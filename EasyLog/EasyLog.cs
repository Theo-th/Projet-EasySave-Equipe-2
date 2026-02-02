using Projet_EasySave.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Projet_EasySave.Models;

namespace Projet_EasySave.EasyLog
{
    public class JsonLog : BaseLog
    {
        public JsonLog(string logDirectory) : base(logDirectory)
        {
            // Le constructeur parent (base) s'occupe déjà de créer le dossier
        }

        public override void WriteLog(LogRecord record)
        {
            // On vérifie que le record est bien un JsonRecord
            if (record is JsonRecord jsonRecord)
            {
                // Options pour rendre le JSON lisible (indentation)
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(jsonRecord, options);

                // Nom du fichier avec la date du jour (ex: log_20231027.json)
                string fileName = $"log_{DateTime.Now:yyyyMMdd}.json";
                string filePath = Path.Combine(_logDirectory, fileName);

                // Écriture (ajout à la fin du fichier s'il existe déjà)
                // On ajoute Environment.NewLine pour séparer les objets JSON si on en écrit plusieurs
                File.AppendAllText(filePath, jsonString + Environment.NewLine);
            }
            else
            {
                // Si on essaie d'envoyer un autre type de record
                throw new ArgumentException("Le log fourni doit être de type JsonRecord");
            }
        }
    }
}
