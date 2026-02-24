using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Properties;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Service for managing backup job configurations.
    /// Handles persistence and validation of backup jobs.
    /// </summary>
    public class JobConfigService : IJobConfigService
    {
        private string _configFilePath;
        private readonly object _lockObject = new();
        private const int MaxJobs = 5;

        private static readonly JsonSerializerOptions ConfigOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Initializes a new instance of <see cref="JobConfigService"/> with the specified configuration file path.
        /// </summary>
        public JobConfigService(string configFilePath = "jobs_config.json")
        {
            // If it is an absolute path, use it as is, otherwise combine it with BaseDirectory
            _configFilePath = Path.IsPathRooted(configFilePath) 
                ? configFilePath 
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFilePath);
        }

        /// <summary>
        /// Retrieves all backup jobs from the configuration file.
        /// </summary>
        public List<BackupJob> GetAllJobs()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(_configFilePath))
                    {
                        string json = File.ReadAllText(_configFilePath);
                        return JsonSerializer.Deserialize<List<BackupJob>>(json, ConfigOptions) ?? new List<BackupJob>();
                    }
                    else
                    {
                        return new List<BackupJob>();
                    }
                }
                catch
                {
                    return new List<BackupJob>();
                }
            }
        }

        /// <summary>
        /// Retrieves a specific backup job by its index.
        /// </summary>
        public BackupJob? GetJob(int index)
        {
            lock (_lockObject)
            {
                var jobs = GetAllJobs();
                if (index >= 0 && index < jobs.Count)
                {
                    return jobs[index];
                }
                return null;
            }
        }

        /// <summary>
        /// Creates and saves a new backup job.
        /// Returns a tuple indicating success and an optional error message.
        /// </summary>
        public (bool Success, string? ErrorMessage) CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
            lock (_lockObject)
            {
                var jobs = GetAllJobs();

                // if (jobs.Count >= MaxJobs)
                // {
                //     return (false, string.Format(Lang.MaxJobsReached, MaxJobs));
                // }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return (false, Lang.JobNameEmpty);
                }

                if (jobs.Exists(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
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
                jobs.Add(newJob);

                if (SaveJobsList(jobs))
                {
                    return (true, null);
                }
                else
                {
                    return (false, Lang.ConfigSaveError);
                }
            }
        }

        /// <summary>
        /// Removes a backup job from the configuration by its index.
        /// </summary>
        public bool RemoveJob(int index)
        {
            lock (_lockObject)
            {
                var jobs = GetAllJobs();
                if (index >= 0 && index < jobs.Count)
                {
                    jobs.RemoveAt(index);
                    return SaveJobsList(jobs);
                }
                return false;
            }
        }

        /// <summary>
        /// Returns the total number of configured backup jobs.
        /// </summary>
        public int GetJobCount()
        {
            lock (_lockObject)
            {
                return GetAllJobs().Count;
            }
        }

        /// <summary>
        /// Saves the list of backup jobs to the configuration file.
        /// </summary>
        private bool SaveJobsList(List<BackupJob> jobs)
        {
            try
            {
                string json = JsonSerializer.Serialize(jobs, ConfigOptions);
                File.WriteAllText(_configFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the configuration file path without recreating the service.
        /// </summary>
        public void UpdateConfigPath(string newConfigPath)
        {
            lock (_lockObject)
            {
                _configFilePath = Path.IsPathRooted(newConfigPath) 
                    ? newConfigPath 
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, newConfigPath);
            }
        }
    }
}