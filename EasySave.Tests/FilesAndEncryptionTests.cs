using Xunit;
using EasySave.Core.Services;
using EasySave.Core.Models;
using System.IO;
using System.Collections.Generic;

namespace EasySave.Tests
{
    /// <summary>
    /// Contains unit tests for file operations, state persistence, and encryption configuration.
    /// </summary>
    public class FilesAndEncryptionTests
    {
        /// <summary>
        /// Verifies that the backup state repository correctly serializes and writes backup states to a JSON file.
        /// </summary>
        [Fact]
        public void BackupStateRepository_WritesStateCorrectly()
        {
            // Arrange
            string statePath = "test_state.json";
            var repo = new BackupStateRepository();
            repo.SetStatePath(statePath);

            var states = new List<BackupJobState> {
                new BackupJobState {
                    Name = "Job1",
                    State = BackupState.Active,
                    TotalSize = 100,
                    RemainingSize = 50
                    }
            };

            // Act
            repo.UpdateState(states);

            // Assert
            Assert.True(File.Exists(statePath));
            string content = File.ReadAllText(statePath);
            Assert.Contains("Job1", content);
            Assert.Contains("Active", content);

            // Cleanup
            File.Delete(statePath);
        }

        /// <summary>
        /// Verifies that the encryption service correctly formats file extensions by adding a dot prefix if it is missing.
        /// </summary>
        [Fact]
        public void EncryptionService_AddExtension_FormatsCorrectly()
        {
            // Arrange
            var service = EncryptionService.Instance;

            // Act
            service.AddExtension("docx");
            service.AddExtension(".pdf");

            var extensions = service.GetExtensions();

            // Assert
            Assert.Contains(".docx", extensions);
            Assert.Contains(".pdf", extensions);
        }
    }
}