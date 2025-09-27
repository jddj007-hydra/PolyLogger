using Xunit;
using Microsoft.Extensions.Options;
using PolyLogger.Options;
using NSubstitute;

namespace PolyLogger.Tests;

public class FileLoggerProviderTests : IDisposable
{
    private readonly IOptionsMonitor<FileLoggerOptions> _optionsMonitor;
    private readonly FileLoggerOptions _options;
    private FileLoggerProvider? _provider;

    public FileLoggerProviderTests()
    {
        _options = new FileLoggerOptions
        {
            RootPath = Path.Combine(Path.GetTempPath(), "PolyLoggerProviderTests", Guid.NewGuid().ToString()),
            DateFormat = "yyyy-MM-dd",
            CreateCategoryDirectories = true
        };

        _optionsMonitor = Substitute.For<IOptionsMonitor<FileLoggerOptions>>();
        _optionsMonitor.CurrentValue.Returns(_options);
    }

    public void Dispose()
    {
        _provider?.Dispose();
        if (Directory.Exists(_options.RootPath))
        {
            Directory.Delete(_options.RootPath, true);
        }
    }

    [Fact]
    public void CreateLogger_ShouldReturnFileLoggerInstance()
    {
        _provider = new FileLoggerProvider(_optionsMonitor);

        var logger = _provider.CreateLogger("TestCategory");

        Assert.NotNull(logger);
        Assert.IsType<FileLogger>(logger);
    }

    [Fact]
    public void CreateLogger_WithSameCategoryName_ShouldReturnSameInstance()
    {
        _provider = new FileLoggerProvider(_optionsMonitor);
        var categoryName = "TestCategory";

        var logger1 = _provider.CreateLogger(categoryName);
        var logger2 = _provider.CreateLogger(categoryName);

        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void CreateLogger_WithDifferentCategoryNames_ShouldReturnDifferentInstances()
    {
        _provider = new FileLoggerProvider(_optionsMonitor);

        var logger1 = _provider.CreateLogger("Category1");
        var logger2 = _provider.CreateLogger("Category2");

        Assert.NotSame(logger1, logger2);
    }

    [Fact]
    public void Dispose_ShouldClearLoggers()
    {
        _provider = new FileLoggerProvider(_optionsMonitor);

        var logger1 = _provider.CreateLogger("Category1");
        var logger2 = _provider.CreateLogger("Category2");

        Assert.NotNull(logger1);
        Assert.NotNull(logger2);

        _provider.Dispose();

        // After disposal, creating new loggers should still work
        var logger3 = _provider.CreateLogger("Category3");
        Assert.NotNull(logger3);
    }
}