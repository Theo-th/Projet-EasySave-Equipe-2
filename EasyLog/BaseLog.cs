using System;
using System.Collections.Generic;
using System.Text;
using Projet_EasyLog.Models;

namespace Projet_EasySave.EasyLog
{
    /// <summary>
    /// Classe abstraite servant de base au système de journalisation (logging).
    /// Elle gère la configuration du répertoire de stockage et impose une structure pour l'écriture des logs.
    /// </summary>
    public abstract class BaseLog
    {
        protected string _logDirectory;

        /// <summary>
        /// Initialise une nouvelle instance de la classe BaseLog et s'assure que le répertoire de logs existe.
        /// </summary>
        /// <param name="logDirectory">Le chemin complet du répertoire où les fichiers de logs seront stockés.</param>
        public BaseLog(string logDirectory)
        {
            _logDirectory = logDirectory;
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Vérifie l'existence du répertoire de logs et le crée s'il n'existe pas.
        /// </summary>
        protected void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        /// <summary>
        /// Méthode abstraite définissant le contrat d'écriture d'un log.
        /// Les classes dérivées (comme JsonLog) doivent implémenter cette méthode pour définir comment le log est écrit.
        /// </summary>
        /// <param name="record">L'objet contenant les informations à enregistrer.</param>
        public abstract void WriteLog(LogRecord record);
    }
}
