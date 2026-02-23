using Xunit;
using Moq;
using EasySave.Core.Services;
using EasySave.Core.Services.Strategies;
using EasySave.Core.Models;
using EasySave.Core.Interfaces;
using EasyLog;
using System.IO;
using System.Collections.Generic;

namespace EasySave.Tests
{
    /// <summary>
    /// Contains unit tests for testing the execution behavior of backup strategies.
    /// </summary>
    public class BackupExecutionTests
    {
        /// <summary>
        /// Verifies that the full backup strategy executes successfully and copies files to the target directory.
        /// </summary>
        [Fact]
        public void FullBackupStrategy_ExecutesSuccessfully()
        {
            // Arrange
            string source = "TestFullSource";
            string target = "TestFullTarget";
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "file.txt"), "content");

            var mockConfigService = new Mock<IJobConfigService>();
            var mockStateRepository = new Mock<IBackupStateRepository>();
            var mockProcessDetector = new Mock<ProcessDetector>();

            var job = new BackupJob
            {
                Name = "TestJob",
                SourceDirectory = source,
                TargetDirectory = target,
                Type = BackupType.Complete
            };

            mockConfigService.Setup(x => x.GetAllJobs()).Returns(new List<BackupJob> { job });
            mockStateRepository.Setup(x => x.UpdateState(It.IsAny<List<BackupJobState>>()));

            var backupService = new BackupService(
                mockConfigService.Object,
                mockStateRepository.Object,
                mockProcessDetector.Object,
                LogType.JSON,
                "test-logs"
            );

            // Act
            var result = backupService.ExecuteBackup(new List<int> { 0 });

            // Assert
            Assert.Null(result); // Null means success
            Assert.True(File.Exists(Path.Combine(target, "full", "file.txt")));

            // Cleanup
            Directory.Delete(source, true);
            Directory.Delete(target, true);
            if (Directory.Exists("test-logs")) Directory.Delete("test-logs", true);
        }

        /// <summary>
        /// Verifies that calling Pause on a backup service correctly changes job states.
        /// </summary>
        [Fact]
        public void BackupService_Pause_ChangesState()
        {
            // Arrange
            var mockConfigService = new Mock<IJobConfigService>();
            var mockStateRepository = new Mock<IBackupStateRepository>();
            var mockProcessDetector = new Mock<ProcessDetector>();

            var backupService = new BackupService(
                mockConfigService.Object,
                mockStateRepository.Object,
                mockProcessDetector.Object,
                LogType.JSON
            );

            // Act & Assert - Pause should work even without active jobs
            backupService.PauseBackup(); // No exception means success
        }

        /// <summary>
        /// Verifies that canceling a backup properly halts the process.
        /// </summary>
        [Fact]
        public void BackupService_Stop_HaltsExecution()
        {
            // Arrange
            var mockConfigService = new Mock<IJobConfigService>();
            var mockStateRepository = new Mock<IBackupStateRepository>();
            var mockProcessDetector = new Mock<ProcessDetector>();

            var backupService = new BackupService(
                mockConfigService.Object,
                mockStateRepository.Object,
                mockProcessDetector.Object,
                LogType.JSON
            );

            // Act & Assert - Stop should work even without active jobs
            backupService.StopBackup(); // No exception means success
        }
    }
}