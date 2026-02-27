using System;

namespace EasySave.Core.Models
{
    /// <summary>
    /// Structure représentant un fichier à sauvegarder, avec ses métadonnées pour l'ordonnancement.
    /// </summary>
    public struct FileJob
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string JobName { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsPriority { get; set; }
        public long FileSize { get; set; }
    }
}
