using Projet_EasySave.Models;

namespace Projet_EasySave.Interfaces
{
    /// <summary>
    /// Interface pour le service d'exécution des travaux de sauvegarde.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Événement déclenché à chaque changement de progression d'un travail de sauvegarde.
        /// </summary>
        event Action<BackupJobState>? OnProgressChanged;

        /// <summary>
        /// Exécute un travail de sauvegarde par son indice.
        /// </summary>
        /// <param name="jobIndices">Liste des indices des travaux (0-based)</param>
        /// <returns>Message d'erreur ou null si succès</returns>
        string? ExecuteBackup(List<int> jobIndices);
    }
}