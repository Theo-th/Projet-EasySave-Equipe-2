using Projet_EasySave.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Projet_EasySave.Models;

namespace Projet_EasySave.EasyLog
{
    public abstract class BaseLog
    // classe permettant de definir le repertoire stockant les logs
    {
        protected string _logDirectory;

        public BaseLog(string logDirectory)
        {
            _logDirectory = logDirectory;
            EnsureDirectoryExists();
        }

        protected void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        // Force les enfants à implémenter cette méthode
        public abstract void WriteLog(LogRecord record);
    }
}
