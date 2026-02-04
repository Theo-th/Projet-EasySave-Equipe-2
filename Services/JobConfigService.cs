using Projet_EasySave.Models;
using System.Text.Json;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service de gestion des configurations de travaux de sauvegarde.
    /// Permet de charger, créer, sauvegarder et supprimer des travaux.
    /// </summary>
    public class JobConfigService
    {
        private readonly string _configFilePath;
        private List<BackupJob> _jobs;
        private const int MaxJobs = 5;

        /// <summary>
        /// Initialise une nouvelle instance du service de configuration.
        /// </summary>
        /// <param name="configFilePath">Chemin du fichier de configuration JSON</param>
        public JobConfigService(string configFilePath = "jobs_config.json")
        {
            _configFilePath = configFilePath;
            _jobs = new List<BackupJob>();
            LoadAllJobs();
        }

        /// <summary>
        /// Charge tous les travaux depuis le fichier de configuration JSON.
        /// </summary>
        /// <returns>Liste de tous les travaux chargés</returns>
        public List<BackupJob> LoadAllJobs()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    _jobs = JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();
                }
                else
                {
                    _jobs = new List<BackupJob>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des travaux : {ex.Message}");
                _jobs = new List<BackupJob>();
            }

            return _jobs;
        }

        /// <summary>
        /// Charge un travail spécifique par son indice.
        /// </summary>
        /// <param name="index">Indice du travail (0-based)</param>
        /// <returns>Le travail demandé ou null s'il n'existe pas</returns>
        public BackupJob? LoadJob(int index)
        {
            if (index >= 0 && index < _jobs.Count)
            {
                return _jobs[index];
            }

            return null;
        }

        /// <summary>
        /// Crée et sauvegarde un nouveau travail de sauvegarde.
        /// </summary>
        /// <param name="name">Nom du travail</param>
        /// <param name="sourceDirectory">Répertoire source</param>
        /// <param name="targetDirectory">Répertoire cible</param>
        /// <param name="type">Type de sauvegarde (Complète ou Différentielle)</param>
        /// <returns>true si le travail a été créé avec succès, false sinon</returns>
        public bool CreateJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            // Vérifier le nombre maximum de travaux
            if (_jobs.Count >= MaxJobs)
            {
                Console.WriteLine($"Erreur : Nombre maximum de travaux ({MaxJobs}) atteint.");
                return false;
            }

            // Vérifier que le nom n'existe pas déjà
            if (_jobs.Exists(j => j.Name == name))
            {
                Console.WriteLine($"Erreur : Un travail avec le nom '{name}' existe déjà.");
                return false;
            }

            // Vérifier que les répertoires sont valides
            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"Erreur : Le répertoire source '{sourceDirectory}' n'existe pas.");
                return false;
            }

            var newJob = new BackupJob(name, sourceDirectory, targetDirectory, type);

            _jobs.Add(newJob);

            return SaveJob();
        }

        /// <summary>
        /// Sauvegarde tous les travaux dans le fichier de configuration JSON.
        /// </summary>
        /// <returns>true si la sauvegarde a été effectuée, false sinon</returns>
        public bool SaveJob()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_jobs, options);
                File.WriteAllText(_configFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde des travaux : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprime un travail par son indice.
        /// </summary>
        /// <param name="index">Indice du travail à supprimer (0-based)</param>
        /// <returns>true si le travail a été supprimé, false sinon</returns>
        public bool RemoveJob(int index)
        {
            if (index >= 0 && index < _jobs.Count)
            {
                _jobs.RemoveAt(index);
                return SaveJob();
            }

            return false;
        }

        /// <summary>
        /// Obtient le nombre total de travaux configurés.
        /// </summary>
        /// <returns>Nombre de travaux</returns>
        public int GetJobCount()
        {
            return _jobs.Count;
        }

        /// <summary>
        /// Obtient tous les travaux configurés.
        /// </summary>
        /// <returns>Liste de tous les travaux</returns>
        public List<BackupJob> GetAllJobs()
        {
            return _jobs;
        }
    }
}