using Projet_EasySave.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Projet_EasySave.Models;

namespace Projet_EasySave.EasyLog
{
    /// <summary>
    /// Implémentation du système de log au format JSON.
    /// Cette classe hérite de BaseLog et écrit les logs dans des fichiers journaliers.
    /// </summary>
    public class JsonLog : BaseLog
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe JsonLog avec le répertoire de destination spécifié.
        /// </summary>
        /// <param name="logDirectory">Le chemin complet du répertoire où les fichiers de logs seront stockés.</param>
        public JsonLog(string logDirectory) : base(logDirectory)
        {
        }

        /// <summary>
        /// Écrit un enregistrement de log dans un fichier JSON daté du jour.
        /// </summary>
        /// <param name="record">L'objet contenant les informations de log. Doit être une instance de JsonRecord.</param>
        /// <exception cref="ArgumentException">Levée lorsque le paramètre record n'est pas de type JsonRecord.</exception>
        public override void WriteLog(LogRecord record)
        {
            if (record is JsonRecord jsonRecord)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(jsonRecord, options);

                string fileName = $"log_{DateTime.Now:yyyyMMdd}.json";
                string filePath = Path.Combine(_logDirectory, fileName);

                File.AppendAllText(filePath, jsonString + Environment.NewLine);
            }
            else
            {
                throw new ArgumentException("Le log fourni doit être de type JsonRecord");
            }
        }
    }
}