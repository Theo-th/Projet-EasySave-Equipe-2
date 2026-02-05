using System;

namespace Projet_EasySave.Models
{
    /// <summary>
    /// Représente l'état d'un travail de sauvegarde pour la persistance temps réel
    /// </summary>
    public class BackupJobState
    {
        /// <summary>
        /// Identifiant unique du travail (1 à 5)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom du travail de sauvegarde
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du répertoire source
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du répertoire cible
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// Type de sauvegarde (Complète ou Différentielle)
        /// </summary>
        public BackupType Type { get; set; } = BackupType.Complete;

        /// <summary>
        /// État actuel du travail
        /// </summary>
        public BackupState State { get; set; } = BackupState.Inactive;

        /// <summary>
        /// Horodatage de la dernière action
        /// </summary>
        public DateTime LastActionTimestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Nombre total de fichiers à sauvegarder
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Taille totale des fichiers à sauvegarder (en octets)
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Nombre de fichiers restants à sauvegarder
        /// </summary>
        public int RemainingFiles { get; set; }

        /// <summary>
        /// Taille restante à sauvegarder (en octets)
        /// </summary>
        public long RemainingSize { get; set; }

        /// <summary>
        /// Fichier source en cours de sauvegarde
        /// </summary>
        public string CurrentSourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Fichier cible en cours de sauvegarde
        /// </summary>
        public string CurrentTargetFile { get; set; } = string.Empty;

        /// <summary>
        /// Pourcentage de progression (0-100)
        /// </summary>
        public int ProgressPercentage
        {
            get
            {
                if (TotalFiles == 0) return 0;
                return (int)((TotalFiles - RemainingFiles) * 100.0 / TotalFiles);
            }
        }
    }
}
