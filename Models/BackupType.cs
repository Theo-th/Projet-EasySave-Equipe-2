namespace ProjetEasySave.Models
{
    /// <summary>
    /// Type de sauvegarde
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Sauvegarde complète (tous les fichiers)
        /// </summary>
        Complete,

        /// <summary>
        /// Sauvegarde différentielle (fichiers modifiés uniquement)
        /// </summary>
        Differential
    }
}
