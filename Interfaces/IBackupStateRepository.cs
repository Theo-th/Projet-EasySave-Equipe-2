using Projet_EasySave.Models;
using System.Collections.Generic;

namespace Projet_EasySave.Interfaces
{
    /// <summary>
    /// Interface pour la gestion de la persistance de l'état temps réel des sauvegardes (state.json)
    /// </summary>
    public interface IBackupStateRepository
    {
        /// <summary>
        /// Définit le chemin du fichier d'état
        /// </summary>
        /// <param name="path">Le chemin du fichier d'état</param>
        /// <exception cref="System.ArgumentException">Levée si le chemin est null ou vide</exception>
        void SetStatePath(string path);

        /// <summary>
        /// Met à jour l'état des travaux de sauvegarde dans le fichier state.json
        /// </summary>
        /// <param name="jobs">Liste des états des travaux de sauvegarde à persister</param>
        void UpdateState(List<BackupJobState> jobs);
    }
}