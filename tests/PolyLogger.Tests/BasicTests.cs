using Xunit;
using Microsoft.Extensions.Logging;
using PolyLogger.Options;

namespace PolyLogger.Tests;

public class BasicTests
{
    [Fact]
    public void FileLoggerOptions_DefaultValues_ShouldBeValid()
    {
        var options = new FileLoggerOptions();

        Assert.Equal("logs", options.RootPath);
        Assert.Equal("yyyy-MM-dd", options.DateFormat);
        Assert.True(options.CreateCategoryDirectories);
        Assert.Equal(LogLevel.Information, options.MinLogLevel);
        Assert.False(options.SeparateByLogLevel);
        Assert.Null(options.MaxFileSize);
        Assert.Null(options.FileNameRule);
    }

    [Fact]
    public void FileLoggerOptions_Validate_WithValidOptions_ShouldNotThrow()
    {
        var options = new FileLoggerOptions
        {
            RootPath = "test-logs",
            DateFormat = "yyyy-MM-dd",
            MinLogLevel = LogLevel.Information
        };

        var exception = Record.Exception(() => options.Validate());

        Assert.Null(exception);
    }
}