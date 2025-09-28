using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolyLogger.Options;
using NSubstitute;

namespace PolyLogger.Tests;

public class FileLoggerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IOptionsMonitor<FileLoggerOptions> _optionsMonitor;
    private readonly FileLoggerOptions _options;

    public FileLoggerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "PolyLoggerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _options = new FileLoggerOptions
        {
            RootPath = _testDirectory,
            DateFormat = "yyyy-MM-dd",
            CreateCategoryDirectories = true,
            MinLogLevel = LogLevel.Information
        };

        _optionsMonitor = Substitute.For<IOptionsMonitor<FileLoggerOptions>>();
        _optionsMonitor.CurrentValue.Returns(_options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void IsEnabled_ShouldReturnTrueForLogLevelAboveMinimum()
    {
        var logger = new FileLogger("TestCategory", _optionsMonitor);

        var result = logger.IsEnabled(LogLevel.Information);

        Assert.True(result);
    }

    [Fact]
    public void IsEnabled_ShouldReturnFalseForLogLevelBelowMinimum()
    {
        var logger = new FileLogger("TestCategory", _optionsMonitor);

        var result = logger.IsEnabled(LogLevel.Debug);

        Assert.False(result);
    }

    [Fact]
    public void Log_ShouldCreateDirectoryStructure()
    {
        var logger = new FileLogger("Services.UserService", _optionsMonitor);

        logger.LogInformation("Test message");

        var expectedDirectory = Path.Combine(_testDirectory, "Services", "UserService");
        Assert.True(Directory.Exists(expectedDirectory));
    }

    [Fact]
    public void Log_ShouldCreateLogFile()
    {
        var logger = new FileLogger("TestCategory", _optionsMonitor);

        logger.LogInformation("Test message");

        var expectedFile = Path.Combine(_testDirectory, "TestCategory", $"{DateTimeOffset.Now:yyyy-MM-dd}.log");
        Assert.True(File.Exists(expectedFile));
    }

    [Fact]
    public void Log_ShouldWriteCorrectContent()
    {
        var logger = new FileLogger("TestCategory", _optionsMonitor);
        var testMessage = "This is a test message";

        logger.LogInformation(testMessage);

        var logFile = Directory.GetFiles(Path.Combine(_testDirectory, "TestCategory"), "*.log").First();
        var content = File.ReadAllText(logFile);

        Assert.Contains(testMessage, content);
        Assert.Contains("[INFORMATION]", content);
        Assert.Contains("[TestCategory]", content);
    }

    [Fact]
    public void Log_WithSeparateByLogLevel_ShouldCreateSeparateFiles()
    {
        _options.SeparateByLogLevel = true;
        var logger = new FileLogger("TestCategory", _optionsMonitor);

        logger.LogInformation("Info message");
        logger.LogWarning("Warning message");
        logger.LogError("Error message");

        var categoryDirectory = Path.Combine(_testDirectory, "TestCategory");
        Assert.True(File.Exists(Path.Combine(categoryDirectory, "Information.log")));
        Assert.True(File.Exists(Path.Combine(categoryDirectory, "Warning.log")));
        Assert.True(File.Exists(Path.Combine(categoryDirectory, "Error.log")));
    }

    [Fact]
    public void BeginScope_ShouldReturnNull()
    {
        var logger = new FileLogger("TestCategory", _optionsMonitor);

        var result = logger.BeginScope("test scope");

        Assert.Null(result);
    }
}