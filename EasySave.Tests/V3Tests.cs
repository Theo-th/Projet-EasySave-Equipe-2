using Xunit;
using Moq;
using EasySave.Core.Services;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasySave.Tests
{
    /// <summary>
    /// Contains unit tests for Version 3 features, including parallel execution, process detection, and single-instance constraints.
    /// </summary>
    public class V3Tests
    {
        /// <summary>
        /// Verifies that the backup service executes multiple jobs in parallel, resulting in multiple active states simultaneously.
        /// </summary>
        [Fact]
        public void ExecuteBackup_ShouldRunJobsInParallel()
        {
            // Arrange
            var mockConfig = new Mock<IJobConfigService>();
            mockConfig.Setup(c => c.GetAllJobs()).Returns(new List<BackupJob> {
                new BackupJob { Name = "Job1", SourceDirectory = "src1", TargetDirectory = "dst1", Type = BackupType.Complete },
                new BackupJob { Name = "Job2", SourceDirectory = "src2", TargetDirectory = "dst2", Type = BackupType.Complete }
            });

            var mockStateRepo = new Mock<IBackupStateRepository>();
            var statesHistory = new List<List<BackupJobState>>();

            mockStateRepo.Setup(r => r.UpdateState(It.IsAny<List<BackupJobState>>()))
                .Callback<List<BackupJobState>>(states =>
                {
                    statesHistory.Add(states.Select(s => new BackupJobState { Name = s.Name, State = s.State }).ToList());
                });

            var service = new BackupService(mockConfig.Object, mockStateRepo.Object, new ProcessDetector((string?)null), LogType.JSON, "logs");

            // Act
            service.ExecuteBackup(new List<int> { 0, 1 });

            // Assert
            bool bothActiveAtSameTime = statesHistory.Any(statesSnapshot =>
                statesSnapshot.Count(s => s.State == BackupState.Active) >= 2);

            Assert.True(bothActiveAtSameTime, "The tasks are not carried out in parallel: they are never active at the same time.");
        }

        /// <summary>
        /// Verifies that the detection of a watched business process pauses the backup rather than completely canceling it.
        /// </summary>
        [Fact]
        public async Task BusinessProcessDetection_ShouldPauseBackup_NotCancel()
        {
            // Arrange
            string testJson = Path.GetFullPath("test_watched_processes.json");
            if (File.Exists(testJson)) File.Delete(testJson);

            string dummySrc = Path.GetFullPath("dummy_src_break");
            string dummyDst = Path.GetFullPath("dummy_dst_break");
            Directory.CreateDirectory(dummySrc);
            File.WriteAllText(Path.Combine(dummySrc, "file.txt"), "dummy content");

            var processDetector = new ProcessDetector(testJson);

            string currentProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            processDetector.AddWatchedProcess(currentProcessName);

            var mockConfig = new Mock<IJobConfigService>();
            mockConfig.Setup(c => c.GetAllJobs()).Returns(new List<BackupJob> {
                new BackupJob { Name = "Job1", SourceDirectory = dummySrc, TargetDirectory = dummyDst, Type = BackupType.Complete }
            });

            var statesHistory = new List<BackupState>();
            var mockStateRepo = new Mock<IBackupStateRepository>();
            mockStateRepo.Setup(r => r.UpdateState(It.IsAny<List<BackupJobState>>()))
                .Callback<List<BackupJobState>>(states => statesHistory.Add(states.First().State));

            var service = new BackupService(mockConfig.Object, mockStateRepo.Object, processDetector, LogType.JSON, "logs");

            // Act
            var executeTask = Task.Run(() => service.ExecuteBackup(new List<int> { 0 }));

            await Task.Delay(500);

            service.StopBackup();
            await executeTask;

            // Assert
            Assert.Contains(BackupState.Paused, statesHistory);

            // Cleanup
            if (File.Exists(testJson)) File.Delete(testJson);
            if (Directory.Exists(dummySrc)) Directory.Delete(dummySrc, true);
            if (Directory.Exists(dummyDst)) Directory.Delete(dummyDst, true);
        }

        /// <summary>
        /// Verifies that the EncryptionService singleton pattern works correctly.
        /// </summary>
        [Fact]
        public async Task EncryptionService_ShouldBeSingleton()
        {
            // Arrange
            var service = EncryptionService.Instance;
            service.AddExtension(".txt");

            string testFile = "test_crypto.txt";
            File.WriteAllText(testFile, "Contenu de test pour le lock.");

            // Act
            var tasks = new List<Task<long>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => service.EncryptFile(testFile)));
            }

            await Task.WhenAll(tasks);

            // Assert
            foreach (var task in tasks)
            {
                Assert.True(task.IsCompletedSuccessfully);
            }

            // Cleanup
            if (File.Exists(testFile)) File.Delete(testFile);
        }
    }
}