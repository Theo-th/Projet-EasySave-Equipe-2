using Projet_EasySave.Services;
using Projet_EasySave.EasyLog;
using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;

namespace Projet_EasySave.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion de la console.
    /// Orchestre la communication entre le modèle (BackupJob) et la vue (console).
    /// </summary>
    public class ViewModelConsole
    {
        private readonly IJobConfigService _configService;
        private readonly IBackupService _backupService;  // ✅ Interface

        /// <summary>
        /// Initialise une nouvelle instance du ViewModel avec injection de dépendances.
        /// </summary>
        public ViewModelConsole(IJobConfigService? configService = null, IBackupStateRepository? stateRepository = null)
        {
            _configService = configService ?? new JobConfigService();
            var repo = stateRepository ?? new BackupStateRepository();

            // Créer le fichier state.json avec un état vide au démarrage si nécessaire
            repo.UpdateState(new List<BackupJobState>());

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            JsonLog myLogger = new JsonLog(logPath);

            _backupService = new BackupService(_configService, myLogger, repo);
        }

        /// <summary>
        /// Crée un nouveau travail de sauvegarde avec les paramètres fournis.
        /// </summary>
        /// <returns>Tuple indiquant le succès et un message d'erreur éventuel</returns>
        public (bool Success, string? ErrorMessage) CreateJob(string? name, string? source, string? destination, BackupType type)
        {
            // Validation des entrées
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Le nom du travail est requis.");

            if (string.IsNullOrWhiteSpace(source))
                return (false, "Le répertoire source est requis.");

            if (string.IsNullOrWhiteSpace(destination))
                return (false, "Le répertoire de destination est requis.");

            return _configService.CreateJob(name.Trim(), source.Trim(), destination.Trim(), type);
        }

        /// <summary>
        /// Exécute un travail de sauvegarde spécifié par son indice.
        /// </summary>
        public string? ExecuteJob(int jobIndex)
        {
            return _backupService.ExecuteBackup(jobIndex);
        }

        /// <summary>
        /// Exécute plusieurs travaux de sauvegarde spécifiés par leurs indices.
        /// </summary>
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
        public bool DeleteJob(int jobIndex)
        {
            return _configService.RemoveJob(jobIndex);
        }

        /// <summary>
        /// Récupère les noms de tous les travaux de sauvegarde configurés.
        /// </summary>
        public List<string> GetAllJobs()
        {
            var jobs = _configService.GetAllJobs();
            return jobs.ConvertAll(job => job.Name);
        }

        /// <summary>
        /// Récupère le nom et le type d'un travail spécifique par son indice.
        /// </summary>
        public string? GetJob(int jobIndex)
        {
            var job = _configService.LoadJob(jobIndex);
            return job != null ? $"{job.Name} -- {job.Type}" : null;
        }
    }
}
