using System.Text.Json.Serialization;

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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du répertoire source à sauvegarder.
        /// </summary>
        public string SourceDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du répertoire cible où la sauvegarde sera effectuée.
        /// </summary>
        public string TargetDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Type de sauvegarde : Complete ou Differential.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BackupType Type { get; set; } = BackupType.Complete;

        /// <summary>
        /// Constructeur par défaut requis pour la désérialisation JSON.
        /// </summary>
        public BackupJob() { }

        /// <summary>
        /// Initialise une nouvelle instance de la classe BackupJob avec les paramètres spécifiés.
        /// </summary>
        public BackupJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }
    }
}
