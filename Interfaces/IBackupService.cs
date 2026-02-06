namespace Projet_EasySave.Interfaces
{
    /// <summary>
    /// Interface pour le service d'exécution des travaux de sauvegarde.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Exécute un travail de sauvegarde par son indice.
        /// </summary>
        /// <param name="jobIndex">Indice du travail (0-based)</param>
        /// <returns>Message d'erreur ou null si succès</returns>
        string? ExecuteBackup(int jobIndex);
    }
}