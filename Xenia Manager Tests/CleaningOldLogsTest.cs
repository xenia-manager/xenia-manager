using System;
using System.IO;
using NUnit.Framework;

namespace Xenia_Manager_Tests
{
    public class Tests
    {
        private string _testLogDirectory;

        private void CleanUpOldLogFiles(string logDirectory, TimeSpan retentionPeriod)
        {
            string[] logFiles = Directory.GetFiles(logDirectory, "Log-*.txt");
            DateTime currentTime = DateTime.UtcNow;

            foreach (string logFile in logFiles)
            {
                FileInfo fileInfo = new FileInfo(logFile);
                if (fileInfo.CreationTimeUtc < currentTime - retentionPeriod)
                {
                    fileInfo.Delete();
                }
            }
        }

        [SetUp]
        public void Setup()
        {
            // Create a temporary directory for testing
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "TestLogDirectory");
            Directory.CreateDirectory(_testLogDirectory);

            // Create some sample log files with different creation times
            CreateLogFile("Log-1.txt", DateTime.UtcNow - TimeSpan.FromDays(5)); // Should be deleted
            CreateLogFile("Log-2.txt", DateTime.UtcNow - TimeSpan.FromDays(8)); // Should be deleted
            CreateLogFile("Log-3.txt", DateTime.UtcNow - TimeSpan.FromDays(1)); // Should not be deleted
        }

        private void CreateLogFile(string fileName, DateTime creationTime)
        {
            string filePath = Path.Combine(_testLogDirectory, fileName);
            File.WriteAllText(filePath, "Sample log content");
            File.SetCreationTimeUtc(filePath, creationTime);
        }

        [Test]
        public void TestLogFile1()
        {
            // Arrange
            TimeSpan retentionPeriod = TimeSpan.FromDays(7);

            // Act
            CleanUpOldLogFiles(_testLogDirectory, retentionPeriod);

            // Assert
            Assert.That(File.Exists(Path.Combine(_testLogDirectory, "Log-1.txt")), Is.True);

        }

        [Test]
        public void TestLogFile2()
        {
            // Arrange
            TimeSpan retentionPeriod = TimeSpan.FromDays(7);

            // Act
            CleanUpOldLogFiles(_testLogDirectory, retentionPeriod);

            // Assert
            Assert.That(File.Exists(Path.Combine(_testLogDirectory, "Log-2.txt")), Is.False);

        }

        [Test]
        public void TestLogFile3()
        {
            // Arrange
            TimeSpan retentionPeriod = TimeSpan.FromDays(7);

            // Act
            CleanUpOldLogFiles(_testLogDirectory, retentionPeriod);

            // Assert
            Assert.That(File.Exists(Path.Combine(_testLogDirectory, "Log-3.txt")), Is.True);

        }

        [TearDown]
        public void Cleanup()
        {
            // Clean up test directory after tests
            Directory.Delete(_testLogDirectory, true);
        }
    }
}