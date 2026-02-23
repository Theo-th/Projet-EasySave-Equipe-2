using Xunit;
using Moq;
using EasySave.Core.Services;
using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;
using System.Linq;

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

            var service = new BackupService(mockConfig.Object, mockStateRepo.Object, new ProcessDetector(), LogType.JSON, "logs");

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
        public void BusinessProcessDetection_ShouldPauseBackup_NotCancel()
        {
            // Arrange
            var processDetector = new ProcessDetector();
            var mockConfig = new Mock<IJobConfigService>();
            mockConfig.Setup(c => c.GetAllJobs()).Returns(new List<BackupJob> {
                new BackupJob { Name = "Job1", SourceDirectory = "src", TargetDirectory = "dst", Type = BackupType.Complete }
            });

            var mockStateRepo = new Mock<IBackupStateRepository>();
            BackupState finalState = BackupState.Inactive;

            mockStateRepo.Setup(r => r.UpdateState(It.IsAny<List<BackupJobState>>()))
                .Callback<List<BackupJobState>>(states => finalState = states.First().State);

            var service = new BackupService(mockConfig.Object, mockStateRepo.Object, processDetector, LogType.JSON, "logs");

            // Act
            var eventArgs = new ProcessStatusChangedEventArgs { Process = new DetectedProcess { ProcessName = "calculator" }, IsRunning = true };

            var methodInfo = typeof(BackupService).GetMethod("OnWatchedProcessStatusChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (methodInfo != null)
            {
                methodInfo.Invoke(service, new object[] { this, eventArgs });
            }

            // Assert
            Assert.Equal(BackupState.Paused, finalState);
        }

        /// <summary>
        /// Verifies that the CryptoSoft encryption tool is restricted to a single instance execution.
        /// </summary>
        [Fact]
        public void CryptoSoft_ShouldBeMonoInstance()
        {
            // Arrange
            var service = EncryptionService.Instance;

            // Act

            // Assert
            Assert.Fail("CryptoSoft is not protected by a Mutex (Single-instance).");
        }
    }
}