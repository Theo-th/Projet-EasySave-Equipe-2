using Projet_EasySave.Services;
using Projet_EasySave.EasyLog;
using Projet_EasySave.Interfaces;
using Projet_EasySave.Models;

namespace Projet_EasySave.ViewModels
{
    public class ViewModelConsole
    {
        private readonly IJobConfigService _configService;

        private IBackupService _backupService = null!;

        private readonly IBackupStateRepository _stateRepository;

        public string CurrentLogFormat { get; private set; } = "JSON";

        public ViewModelConsole(IJobConfigService? configService = null, IBackupStateRepository? stateRepository = null)
        {
            _configService = configService ?? new JobConfigService();

            _stateRepository = stateRepository ?? new BackupStateRepository();

            _stateRepository.UpdateState(new List<BackupJobState>());

            SetBackupServiceStrategy("JSON");
        }

        public (bool Success, string? ErrorMessage) CreateJob(string? name, string? source, string? destination, BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name)) return (false, "Le nom du travail est requis.");
            if (string.IsNullOrWhiteSpace(source)) return (false, "Le répertoire source est requis.");
            if (string.IsNullOrWhiteSpace(destination)) return (false, "Le répertoire de destination est requis.");
            return _configService.CreateJob(name.Trim(), source.Trim(), destination.Trim(), type);
        }

        public string? ExecuteJob(int jobIndex)
        {
            return _backupService.ExecuteBackup(jobIndex);
        }

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

        public bool DeleteJob(int jobIndex)
        {
            return _configService.RemoveJob(jobIndex);
        }

        public List<string> GetAllJobs()
        {
            var jobs = _configService.GetAllJobs();
            return jobs.ConvertAll(job => job.Name);
        }

        public string? GetJob(int jobIndex)
        {
            var job = _configService.LoadJob(jobIndex);
            return job != null ? $"{job.Name} -- {job.Type}" : null;
        }

        public string? GetTypeJob(int typeJob)
        {
            return "";
        }


        private void SetBackupServiceStrategy(string format)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            BaseLog myLogger;

            if (format == "XML")
            {
                myLogger = new XmlLog(logPath);
            }
            else
            {
                myLogger = new JsonLog(logPath);
            }

            _backupService = new BackupService(_configService, myLogger, _stateRepository);

            CurrentLogFormat = format;
        }

        public void ChangeLogFormat(string format)
        {
            if (format != "JSON" && format != "XML") return;

            SetBackupServiceStrategy(format);
        }
    }
}