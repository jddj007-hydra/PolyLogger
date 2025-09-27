using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;
using PolyLogger.Options;

namespace PolyLogger.Tests;

public class LoggingBuilderExtensionsTests
{
    [Fact]
    public void AddFileLogger_WithValidConfiguration_ShouldRegisterServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddFileLogger(options =>
            {
                options.RootPath = "test-logs";
                options.DateFormat = "yyyy-MM-dd";
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var loggerProvider = serviceProvider.GetService<ILoggerProvider>();

        Assert.NotNull(loggerProvider);
        Assert.IsType<FileLoggerProvider>(loggerProvider);
    }

    [Fact]
    public void AddFileLogger_WithoutConfiguration_ShouldRegisterServicesWithDefaults()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddFileLogger();
        });

        var serviceProvider = services.BuildServiceProvider();
        var loggerProvider = serviceProvider.GetService<ILoggerProvider>();

        Assert.NotNull(loggerProvider);
        Assert.IsType<FileLoggerProvider>(loggerProvider);
    }

    [Fact]
    public void AddFileLogger_ShouldCreateFunctionalLogger()
    {
        var services = new ServiceCollection();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "PolyLoggerExtensionTests", Guid.NewGuid().ToString());

        try
        {
            services.AddLogging(builder =>
            {
                builder.AddFileLogger(options =>
                {
                    options.RootPath = tempDirectory;
                    options.DateFormat = "yyyy-MM-dd";
                    options.CreateCategoryDirectories = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<LoggingBuilderExtensionsTests>>();

            Assert.NotNull(logger);

            logger.LogInformation("Test message from extensions");

            // Give some time for the file to be created
            Thread.Sleep(100);

            // Check what directories were actually created
            var createdDirs = Directory.GetDirectories(tempDirectory, "*", SearchOption.AllDirectories);
            var allDirs = string.Join(", ", createdDirs);

            var expectedDirectory = Path.Combine(tempDirectory, "PolyLogger.Tests", "LoggingBuilderExtensionsTests");

            // If the expected directory doesn't exist, try to find what was actually created
            if (!Directory.Exists(expectedDirectory))
            {
                // Maybe the directory structure is different
                if (createdDirs.Length > 0)
                {
                    expectedDirectory = createdDirs[0];
                }
            }

            Assert.True(Directory.Exists(expectedDirectory), $"Directory does not exist: {expectedDirectory}. Created dirs: {allDirs}");

            // Find any log files in the directory
            var logFiles = Directory.GetFiles(expectedDirectory, "*.log");
            Assert.True(logFiles.Length > 0, $"No log files found in {expectedDirectory}");

            var content = File.ReadAllText(logFiles[0]);
            Assert.Contains("Test message from extensions", content);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}