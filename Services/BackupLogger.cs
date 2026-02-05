using Projet_EasySave.EasyLog;
using Projet_EasySave.Models;
using System;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Classe helper pour centraliser la logique de logging des opérations de sauvegarde
    /// </summary>
    public static class BackupLogger
    {
        /// <summary>
        /// Enregistre une opération de fichier dans les logs
        /// </summary>
        public static void LogFileOperation(JsonLog log, string name, string source, 
            string target, long size, long elapsedMs, string message)
        {
            log.WriteLog(new JsonRecord
            {
                Timestamp = DateTime.Now,
                Name = name,
                Source = source,
                Target = target,
                Size = size,
                Time = elapsedMs,
                Message = message
            });
        }
    }
}
