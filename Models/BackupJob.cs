using System;
using System.Collections.Generic;

namespace Projet_EasySave.Models
{
    /// <summary>
    /// Représente un travail de sauvegarde avec ses propriétés (nom, répertoires source/cible et type).
    /// </summary>
    public class BackupJob
    {
        /// <summary>
        /// Nom unique de la sauvegarde.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Chemin du répertoire source à sauvegarder.
        /// </summary>
        public string SourceDirectory { get; set; }

        /// <summary>
        /// Chemin du répertoire cible où la sauvegarde sera effectuée.
        /// </summary>
        public string TargetDirectory { get; set; }

        /// <summary>
        /// Type de sauvegarde : Complete ou Differential.
        /// </summary>
        public string Type { get; set; }


        /// <summary>
        /// Initialise une nouvelle instance de la classe BackupJob avec les paramètres spécifiés.
        /// </summary>
        /// <param name="name">Nom de la sauvegarde</param>
        /// <param name="sourceDirectory">Répertoire source</param>
        /// <param name="targetDirectory">Répertoire cible</param>
        /// <param name="type">Type de sauvegarde</param>
        public BackupJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }
    }
}
