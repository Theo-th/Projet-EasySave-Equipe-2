using Projet_EasySave.EasyLog;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using EasySave.Core.Properties;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Service for executing backup jobs.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IJobConfigService _configService;
        private readonly JsonLog _log;
        private readonly IBackupStateRepository _stateRepository;

        public BackupService(IJobConfigService configService, JsonLog log, IBackupStateRepository stateRepository)
        {
            _configService = configService;
            _log = log;
            _stateRepository = stateRepository;
        }

        /// <summary>
        /// Executes a backup job by its index.
        /// </summary>
        /// <param name="jobIndex">Job index (0-based)</param>
        /// <returns>Error message or null on success</returns>
        public string? ExecuteBackup(int jobIndex)
        {
            BackupJob? job = _configService.LoadJob(jobIndex);
            if (job == null)
                return string.Format(Lang.JobIndexNotFound, jobIndex);

            return ExecuteBackup(job, jobIndex);
        }

        /// <summary>
        /// Executes a backup job.
        /// </summary>
        /// <param name="job">The job to execute</param>
        /// <param name="jobIndex">Job index</param>
        /// <returns>Information or error message, null on success</returns>
        private string? ExecuteBackup(BackupJob job, int jobIndex = 0)
        {
            var (totalFiles, totalSize) = GetFilesInfo(job.SourceDirectory);

            var jobState = new BackupJobState
            {
                Id = jobIndex + 1,
                Name = job.Name,
                SourcePath = job.SourceDirectory,
                TargetPath = job.TargetDirectory,
                Type = job.Type == BackupType.Complete ? BackupType.Complete : BackupType.Differential,
                State = BackupState.Active,
                LastActionTimestamp = DateTime.Now,
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = totalFiles,
                RemainingSize = totalSize,
                CurrentSourceFile = "",
                CurrentTargetFile = ""
            };

            _stateRepository.UpdateState(new List<BackupJobState> { jobState });

            try
            {
                IBackupStrategy strategy = job.Type switch
                {
                    BackupType.Complete => new FullBackupStrategy(),
                    BackupType.Differential => new DifferentialBackupStrategy(),
                    _ => new FullBackupStrategy()
                };

                var result = strategy.ProcessBackup(job.SourceDirectory, job.TargetDirectory, job.Name, _log);

                jobState.State = BackupState.Completed;
                jobState.RemainingFiles = 0;
                jobState.RemainingSize = 0;
                jobState.LastActionTimestamp = DateTime.Now;
                _stateRepository.UpdateState(new List<BackupJobState> { jobState });

                return result;
            }
            catch (Exception ex)
            {
                jobState.State = BackupState.Error;
                jobState.LastActionTimestamp = DateTime.Now;
                _stateRepository.UpdateState(new List<BackupJobState> { jobState });

                return string.Format(Lang.SaveErrorWithException, ex.Message);
            }
        }

        private (int count, long size) GetFilesInfo(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                return (files.Length, files.Sum(f => new FileInfo(f).Length));
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}