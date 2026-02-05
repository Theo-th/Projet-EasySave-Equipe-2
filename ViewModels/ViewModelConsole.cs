using Projet_EasySave.Services;
using Projet_EasySave.EasyLog;
using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;
using System.Collections.Generic;


namespace Projet_EasySave.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion de la console.
    /// Orchestre la communication entre le modèle (BackupJob) et la vue (console).
    /// </summary>
    public class ViewModelConsole
    {
        private JobConfigService _configService;
        private BackupService _backupService;
        private IBackupStateRepository _stateRepository;

        /// <summary>
        /// Initialise une nouvelle instance du ViewModel pour la console.
        /// </summary>
        public ViewModelConsole()
        {
            _configService = new JobConfigService();

            // Initialisation du système d'état temps réel
            _stateRepository = new BackupStateRepository();
            
            // Créer le fichier state.json avec un état vide au démarrage si nécessaire
            _stateRepository.UpdateState(new List<BackupJobState>());

            // AppDomain.CurrentDomain.BaseDirectory permet d'avoir le dossier de l'exe
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            JsonLog myLogger = new JsonLog(logPath);

            _backupService = new BackupService(_configService, myLogger, _stateRepository);
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
        /// <returns>Message d'information ou d'erreur, null si succès sans message</returns>
        public string? ExecuteJob(int jobIndex)
        {
            return _backupService.ExecuteBackup(jobIndex);
        }

        /// <summary>
        /// Exécute plusieurs travaux de sauvegarde spécifiés par leurs indices.
        /// </summary>
        /// <param name="jobIndices">Collection des indices des travaux à exécuter</param>
        /// <returns>Liste des messages pour chaque travail (null si succès sans message)</returns>
        public List<(int Index, string? Message)> ExecuteJobs(List<int> jobIndices)
        {
            var results = new List<(int, string?)>();
            foreach (int index in jobIndices)
            {
                string? message = _backupService.ExecuteBackup(index);
                results.Add((index, message));
            }
            return results;
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
        /// Récupère le nom et le type d'un travail spécifique par son indice.
        /// </summary>
        /// <param name="jobIndex">Indice du travail (0-based)</param>
        /// <returns>Une chaîne au format "Name -- Type" ou null s'il n'existe pas</returns>
        public string? GetJob(int jobIndex)
        {
            var job = _configService.LoadJob(jobIndex);
            return job != null ? $"{job.Name} -- {job.Type}" : null;
        }
    }
}
