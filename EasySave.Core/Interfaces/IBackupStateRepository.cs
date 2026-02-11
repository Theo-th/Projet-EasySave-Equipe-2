using EasySave.Core.Models;
using System.Collections.Generic;

namespace EasySave.Core.Interfaces
{
    /// <summary>
    /// Interface pour la persistance de l'état temps réel des sauvegardes (state.json)
    /// </summary>
    public interface IBackupStateRepository
    {
        /// <summary>
        /// Met à jour le fichier state.json avec l'état actuel des travaux
        /// </summary>
        /// <param name="jobs">Liste des états de travaux de sauvegarde</param>
        void UpdateState(List<BackupJobState> jobs);

        /// <summary>
        /// Définit le chemin où sera sauvegardé le fichier state.json
        /// </summary>
        /// <param name="path">Chemin du fichier state.json</param>
        void SetStatePath(string path);
    }
}
