using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Properties;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Service for managing backup job configurations.
    /// </summary>
    public class JobConfigService : IJobConfigService
    {
        private readonly string _configFilePath;
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
        }

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
        /// </summary>
        /// <returns>Tuple indicating success and an optional error message</returns>
        public (bool Success, string? ErrorMessage) CreateJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
            lock (_lockObject)
            {
                var jobs = GetAllJobs();

                if (jobs.Count >= MaxJobs)
                {
                    return (false, string.Format(Lang.MaxJobsReached, MaxJobs));
                }

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

        public int GetJobCount()
        {
            lock (_lockObject)
            {
                return GetAllJobs().Count;
            }
        }

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
    }
}