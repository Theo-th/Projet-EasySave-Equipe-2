using Xunit;
using EasySave.Core.Services;
using EasySave.Core.Models;
using System.IO;

namespace EasySave.Tests
{
    /// <summary>
    /// Contains unit tests for the creation, deletion, and management of backup jobs.
    /// </summary>
    public class JobManagementTests
    {
        private readonly string _testConfigPath = "test_jobs_config.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="JobManagementTests"/> class and cleans up any existing test configuration files.
        /// </summary>
        public JobManagementTests()
        {
            if (File.Exists(_testConfigPath)) File.Delete(_testConfigPath);
        }

        /// <summary>
        /// Verifies that creating a job with valid data successfully adds the job to the configuration.
        /// </summary>
        [Fact]
        public void CreateJob_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var service = new JobConfigService(_testConfigPath);
            Directory.CreateDirectory("TestSource");

            // Act
            var result = service.CreateJob("TestJob", "TestSource", "TestDest", BackupType.Complete);

            // Assert
            Assert.True(result.Success);
            Assert.Single(service.GetAllJobs());

            // Cleanup
            Directory.Delete("TestSource");
        }

        /// <summary>
        /// Verifies that deleting a job with a valid index successfully removes it from the configuration.
        /// </summary>
        [Fact]
        public void DeleteJob_WithValidIndex_RemovesJob()
        {
            // Arrange
            var service = new JobConfigService(_testConfigPath);
            Directory.CreateDirectory("TestSource");
            service.CreateJob("JobToDelete", "TestSource", "TestDest", BackupType.Complete);

            // Act
            bool isDeleted = service.RemoveJob(0);

            // Assert
            Assert.True(isDeleted);
            Assert.Empty(service.GetAllJobs());

            // Cleanup
            Directory.Delete("TestSource");
        }
    }
}