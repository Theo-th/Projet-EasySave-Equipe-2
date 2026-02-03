using ProjetEasySave.Interfaces;
using ProjetEasySave.Services;
using ProjetEasySave.Models;
using System;

namespace ProjetEasySave
{
    /// <summary>
    /// Point d'entrée principal de l'application EasySave
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            
            Console.WriteLine("EasySave - Application de sauvegarde");
            
            
            IBackupStateRepository stateRepository = new BackupStateRepository();
            Console.WriteLine("Repository d'état initialisé.");
        }
    }
}
 