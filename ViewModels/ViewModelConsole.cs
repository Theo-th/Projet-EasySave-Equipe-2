using Projet_EasySave.Models;
using Projet_EasySave.Services;
using System;
using System.Collections.Generic;
using System.Text.Json;
using static System.Reflection.Metadata.BlobBuilder;

namespace Projet_EasySave.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion de la console.
    /// Orchestre la communication entre le modèle (BackupJob) et la vue (console).
    /// </summary>
    public class ViewModelConsole
    {
        private JobConfigService _configService;

        /// <summary>
        /// Initialise une nouvelle instance du ViewModel pour la console.
        /// </summary>
        public ViewModelConsole()
        {
            _configService = new JobConfigService();
        }

        /// <summary>
        /// Crée un nouveau travail de sauvegarde avec les paramètres fournis.
        /// </summary>
        /// <param name="name">Nom du travail</param>
        /// <param name="source">Répertoire source</param>
        /// <param name="destination">Répertoire de destination</param>
        /// <param name="type">Type de sauvegarde (Complète ou Différentielle)</param>
        /// <returns>true si la création a réussi, false sinon</returns>
        public bool CreateJob(string name, string source, string destination, string type)
        {
            return _configService.CreateJob(name, source, destination, type);
        }

        /// <summary>
        /// Exécute un travail de sauvegarde spécifié par son indice.
        /// </summary>
        /// <param name="jobIndex">Indice du travail à exécuter (0-based)</param>
        /// <returns>true si l'exécution a réussi, false sinon</returns>
        public bool ExecuteJob(int jobIndex)
        {
            return false;
        }

        /// <summary>
        /// Exécute plusieurs travaux de sauvegarde spécifiés par leurs indices.
        /// </summary>
        /// <param name="jobIndices">Collection des indices des travaux à exécuter</param>
        /// <returns>true si tous les travaux ont réussi, false sinon</returns>
        public bool ExecuteJobs(List<int> jobIndices)
        {
            return true;
        }

        /// <summary>
        /// Supprime un travail de sauvegarde spécifié par son indice.
        /// </summary>
        /// <param name="jobIndex">Indice du travail à supprimer (0-based)</param>
        /// <returns>true si la suppression a réussi, false sinon</returns>
        public bool DeleteJob(int jobIndex)
        {
            return _configService.RemoveJob(jobIndex);
        }

        /// <summary>
        /// Récupère les noms de tous les travaux de sauvegarde configurés.
        /// </summary>
        /// <returns>Liste des noms de tous les travaux</returns>
        public List<string> GetAllJobs()
        {
            var jobs = _configService.GetAllJobs();
            return jobs.ConvertAll(job => job.Name);
        }

        /// <summary>
        /// Récupère le nom d'un travail spécifique par son indice.
        /// </summary>
        /// <param name="jobIndex">Indice du travail (0-based)</param>
        /// <returns>Le nom du travail demandé ou null s'il n'existe pas</returns>
        public string? GetJob(int jobIndex)
        {
            var job = _configService.LoadJob(jobIndex);
            return job?.Name;
        }
    }
}
