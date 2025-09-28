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

            using var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<LoggingBuilderExtensionsTests>>();

            Assert.NotNull(logger);

            logger.LogInformation("Test message from extensions");

            // Force disposal to ensure files are flushed
            serviceProvider.Dispose();

            // Give some time for the file to be created
            Thread.Sleep(200);

            // Check what directories were actually created
            var createdDirs = Directory.GetDirectories(tempDirectory, "*", SearchOption.AllDirectories);
            var allDirs = string.Join(", ", createdDirs);

            // Check if tempDirectory exists and what's in it
            var tempExists = Directory.Exists(tempDirectory);
            var tempFiles = tempExists ? string.Join(", ", Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories)) : "Directory doesn't exist";

            // Find any log files in the temp directory
            var foundLogFiles = tempExists ? Directory.GetFiles(tempDirectory, "*.log", SearchOption.AllDirectories) : new string[0];

            if (foundLogFiles.Length == 0)
            {
                Assert.Fail($"No log files found anywhere in temp directory. Temp dir exists: {tempExists}. Created dirs: {allDirs}. Files: {tempFiles}");
            }

            // If we found log files, use the first one
            var logFile = foundLogFiles[0];

            var content = File.ReadAllText(logFile);
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