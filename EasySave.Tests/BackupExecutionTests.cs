using Xunit;
using Moq;
using EasySave.Core.Services.Strategies;
using EasySave.Core.Models;
using EasyLog;
using System.IO;

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

            var mockLogger = new Mock<BaseLog>("test-path");
            var strategy = new FullBackupStrategy(source, target, BackupType.Complete, "Job1", mockLogger.Object, LogTarget.Local);

            // Act
            var result = strategy.Execute();

            // Assert
            Assert.True(result.Success);
            Assert.True(File.Exists(Path.Combine(target, "full", "file.txt")));

            // Cleanup
            Directory.Delete(source, true);
            Directory.Delete(target, true);
        }

        /// <summary>
        /// Verifies that calling Pause on a backup strategy correctly changes its state and triggers the associated event.
        /// </summary>
        [Fact]
        public void BackupStrategy_Pause_ChangesState()
        {
            // Arrange
            var mockLogger = new Mock<BaseLog>("test_path");
            var strategy = new FullBackupStrategy("src", "dst", BackupType.Complete, "Job", mockLogger.Object, LogTarget.Local);

            bool pauseEventTriggered = false;
            strategy.OnPauseStateChanged += (isPaused) => pauseEventTriggered = isPaused;

            // Act
            strategy.Pause();

            // Assert
            Assert.True(strategy.IsPaused);
            Assert.True(pauseEventTriggered);
        }

        /// <summary>
        /// Verifies that canceling a backup strategy during its execution properly halts the process and returns an error.
        /// </summary>
        [Fact]
        public void BackupStrategy_Cancel_ThrowsExceptionDuringExecution()
        {
            // Arrange
            string source = "TestCancelSource";
            string target = "TestCancelTarget";
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "file.txt"), "content");

            var mockLogger = new Mock<BaseLog>("test_path");
            var strategy = new FullBackupStrategy(source, target, BackupType.Complete, "Job", mockLogger.Object, LogTarget.Local);

            // Act
            strategy.Cancel();
            var result = strategy.Execute();

            // Assert
            Assert.False(result.Success);
            Assert.Contains("cancelled", result.ErrorMessage ?? "", System.StringComparison.OrdinalIgnoreCase);

            // Cleanup
            if (Directory.Exists(source)) Directory.Delete(source, true);
            if (Directory.Exists(target)) Directory.Delete(target, true);
        }
    }
}