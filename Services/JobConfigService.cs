using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;
using Projet_EasySave.Properties;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service de gestion des configurations de travaux de sauvegarde.
    /// Permet de charger, créer, sauvegarder et supprimer des travaux.
    /// </summary>
    public class JobConfigService : IJobConfigService
    {
        private readonly string _configFilePath;
        private List<BackupJob> _jobs;
        private readonly object _lockObject = new();
        private const int MaxJobs = 5;

        private static readonly JsonSerializerOptions ConfigOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Initialise une nouvelle instance du service de configuration.
        /// </summary>
        /// <param name="configFilePath">Chemin du fichier de configuration JSON</param>
        public JobConfigService(string configFilePath = "jobs_config.json")
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFilePath);
            _jobs = new List<BackupJob>();
            LoadAllJobs();
        }

        /// <summary>
        /// Charge tous les travaux depuis le fichier de configuration JSON.
        /// </summary>
        public List<BackupJob> LoadAllJobs()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(_configFilePath))
                    {
                        string json = File.ReadAllText(_configFilePath);
                        _jobs = JsonSerializer.Deserialize<List<BackupJob>>(json, ConfigOptions) ?? new List<BackupJob>();
                    }
                    else
                    {
                        _jobs = new List<BackupJob>();
                    }
                }
                catch
                {
                    _jobs = new List<BackupJob>();
                }

                return _jobs.ToList(); // Retourne une copie
            }
        }

        /// <summary>
        /// Charge un travail spécifique par son indice.
        /// </summary>
        public BackupJob? LoadJob(int index)
        {
            lock (_lockObject)
            {
                if (index >= 0 && index < _jobs.Count)
                {
                    return _jobs[index];
                }
                return null;
            }
        }

        /// <summary>
        /// Crée et sauvegarde un nouveau travail de sauvegarde.
        /// </summary>
        /// <returns>Tuple indiquant le succès et un message d'erreur éventuel</returns>
        public (bool Success, string? ErrorMessage) CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
            lock (_lockObject)
            {
                // Vérifier le nombre maximum de travaux
                if (_jobs.Count >= MaxJobs)
                {
                    return (false, string.Format(Lang.MaxJobsReached, MaxJobs));
                }

                // Vérifier que le nom n'est pas vide
                if (string.IsNullOrWhiteSpace(name))
                {
                    return (false, Lang.JobNameEmpty);
                }

                // Vérifier que le nom n'existe pas déjà
                if (_jobs.Exists(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, string.Format(Lang.JobAlreadyExists, name));
                }

                // Vérifier que le répertoire source est valide
                if (string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    return (false, Lang.SourceDirectoryEmpty);
                }

                if (!Directory.Exists(sourceDirectory))
                {
                    return (false, string.Format(Lang.SourceDirectoryNotFound, sourceDirectory));
                }

                // Vérifier que le répertoire cible n'est pas vide
                if (string.IsNullOrWhiteSpace(targetDirectory))
                {
                    return (false, Lang.TargetDirectoryEmpty);
                }

                var newJob = new BackupJob(name, sourceDirectory, targetDirectory, type);
                _jobs.Add(newJob);

                if (SaveJob())
                {
                    return (true, null);
                }
                else
                {
                    _jobs.Remove(newJob);
                    return (false, Lang.ConfigSaveError);
                }
            }
        }

        /// <summary>
        /// Sauvegarde tous les travaux dans le fichier de configuration JSON.
        /// </summary>
        public bool SaveJob()
        {
            try
            {
                string json = JsonSerializer.Serialize(_jobs, ConfigOptions);
                File.WriteAllText(_configFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Supprime un travail par son indice.
        /// </summary>
        public bool RemoveJob(int index)
        {
            lock (_lockObject)
            {
                if (index >= 0 && index < _jobs.Count)
                {
                    _jobs.RemoveAt(index);
                    return SaveJob();
                }
                return false;
            }
        }

        /// <summary>
        /// Obtient le nombre total de travaux configurés.
        /// </summary>
        public int GetJobCount()
        {
            lock (_lockObject)
            {
                return _jobs.Count;
            }
        }

        /// <summary>
        /// Obtient tous les travaux configurés (copie défensive).
        /// </summary>
        public List<BackupJob> GetAllJobs()
        {
            lock (_lockObject)
            {
                return _jobs.ToList(); // Retourne une copie pour éviter les modifications externes
            }
        }
    }
}