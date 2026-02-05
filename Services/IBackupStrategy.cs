namespace Projet_EasySave.Services
{
    /// <summary>
    /// Interface définissant la stratégie de sauvegarde.
    /// </summary>
    public interface IBackupStrategy
    {
        /// <summary>
        /// Exécute la sauvegarde du répertoire source vers le répertoire cible.
        /// </summary>
        /// <param name="source">Chemin du répertoire source</param>
        /// <param name="target">Chemin du répertoire cible</param>
        /// <returns>Message d'information ou null si aucun message</returns>
        string? ProcessBackup(string source, string target);
    }
}