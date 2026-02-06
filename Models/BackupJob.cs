using System.Text.Json.Serialization;

namespace Projet_EasySave.Models
{
    /// <summary>
    /// Represents a backup job with its properties.
    /// </summary>
    public class BackupJob
    {
        public string Name { get; set; } = string.Empty;

        public string SourceDirectory { get; set; } = string.Empty;

        public string TargetDirectory { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BackupType Type { get; set; } = BackupType.Complete;

        public BackupJob() { }

        public BackupJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
        }
    }
}
