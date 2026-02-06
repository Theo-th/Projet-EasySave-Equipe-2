using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;
using Projet_EasySave.Properties;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projet_EasySave.Services
{
    /// <summary>
    /// Service for managing backup job configurations.
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

        public JobConfigService(string configFilePath = "jobs_config.json")
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFilePath);
            _jobs = new List<BackupJob>();
            LoadAllJobs();
        }

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

                return _jobs.ToList(); // Returns a copy
            }
        }

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
        /// Creates and saves a new backup job.
        /// </summary>
        /// <returns>Tuple indicating success and an optional error message</returns>
        public (bool Success, string? ErrorMessage) CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
            lock (_lockObject)
            {
                if (_jobs.Count >= MaxJobs)
                {
                    return (false, string.Format(Lang.MaxJobsReached, MaxJobs));
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return (false, Lang.JobNameEmpty);
                }

                if (_jobs.Exists(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, string.Format(Lang.JobAlreadyExists, name));
                }

                if (string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    return (false, Lang.SourceDirectoryEmpty);
                }

                if (!Directory.Exists(sourceDirectory))
                {
                    return (false, string.Format(Lang.SourceDirectoryNotFound, sourceDirectory));
                }

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

        public int GetJobCount()
        {
            lock (_lockObject)
            {
                return _jobs.Count;
            }
        }

        public List<BackupJob> GetAllJobs()
        {
            lock (_lockObject)
            {
                return _jobs.ToList();
            }
        }
    }
}