using Projet_EasySave.Models;
using System.Text.Json;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service de gestion des configurations de travaux de sauvegarde.
    /// Permet de charger, cr�er, sauvegarder et supprimer des travaux.
    /// </summary>
    public class JobConfigService
    {
        private readonly string _configFilePath;
        private List<BackupJob> _jobs;
        private const int MaxJobs = 5;

        private static readonly JsonSerializerOptions ConfigOptions = new()
        {
            WriteIndented = true
        };

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
        /// <returns>Liste de tous les travaux charg�s</returns>
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
        /// Charge un travail sp�cifique par son indice.
        /// </summary>
        /// <param name="index">Indice du travail (0-based)</param>
        /// <returns>Le travail demand� ou null s'il n'existe pas</returns>
        public BackupJob? LoadJob(int index)
        {
            if (index >= 0 && index < _jobs.Count)
            {
                return _jobs[index];
            }

            return null;
        }

        /// <summary>
        /// Cr�e et sauvegarde un nouveau travail de sauvegarde.
        /// </summary>
        /// <param name="name">Nom du travail</param>
        /// <param name="sourceDirectory">R�pertoire source</param>
        /// <param name="targetDirectory">R�pertoire cible</param>
        /// <param name="type">Type de sauvegarde (Compl�te ou Diff�rentielle)</param>
        /// <returns>true si le travail a �t� cr�� avec succ�s, false sinon</returns>
        public bool CreateJob(string name, string sourceDirectory, string targetDirectory, string type)
        {
            // V�rifier le nombre maximum de travaux
            if (_jobs.Count >= MaxJobs)
            {
                Console.WriteLine($"Erreur : Nombre maximum de travaux ({MaxJobs}) atteint.");
                return false;
            }

            // V�rifier que le nom n'existe pas d�j�
            if (_jobs.Exists(j => j.Name == name))
            {
                Console.WriteLine($"Erreur : Un travail avec le nom '{name}' existe d�j�.");
                return false;
            }

            // V�rifier que les r�pertoires sont valides
            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"Erreur : Le r�pertoire source '{sourceDirectory}' n'existe pas.");
                return false;
            }

            var newJob = new BackupJob(name, sourceDirectory, targetDirectory, type);

            _jobs.Add(newJob);

            return SaveJob();
        }

        /// <summary>
        /// Sauvegarde tous les travaux dans le fichier de configuration JSON.
        /// </summary>
        /// <returns>true si la sauvegarde a �t� effectu�e, false sinon</returns>
        public bool SaveJob()
        {
            try
            {
                string json = JsonSerializer.Serialize(_jobs, ConfigOptions);
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
        /// <param name="index">Indice du travail � supprimer (0-based)</param>
        /// <returns>true si le travail a �t� supprim�, false sinon</returns>
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
        /// Obtient le nombre total de travaux configur�s.
        /// </summary>
        /// <returns>Nombre de travaux</returns>
        public int GetJobCount()
        {
            return _jobs.Count;
        }

        /// <summary>
        /// Obtient tous les travaux configur�s.
        /// </summary>
        /// <returns>Liste de tous les travaux</returns>
        public List<BackupJob> GetAllJobs()
        {
            return _jobs;
        }
    }
}