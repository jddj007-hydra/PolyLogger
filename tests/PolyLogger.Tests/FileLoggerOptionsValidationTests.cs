using Xunit;
using Microsoft.Extensions.Logging;
using PolyLogger.Options;

namespace PolyLogger.Tests;

public class FileLoggerOptionsValidationTests
{
    [Fact]
    public void Validate_WithNullRootPath_ShouldThrowArgumentException()
    {
        var options = new FileLoggerOptions
        {
            RootPath = null!,
            DateFormat = "yyyy-MM-dd"
        };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.Contains("RootPath cannot be null or empty", exception.Message);
        Assert.Equal("RootPath", exception.ParamName);
    }

    [Fact]
    public void Validate_WithEmptyRootPath_ShouldThrowArgumentException()
    {
        var options = new FileLoggerOptions
        {
            RootPath = "",
            DateFormat = "yyyy-MM-dd"
        };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.Contains("RootPath cannot be null or empty", exception.Message);
        Assert.Equal("RootPath", exception.ParamName);
    }

    [Fact]
    public void Validate_WithInvalidDateFormat_ShouldThrowArgumentException()
    {
        var options = new FileLoggerOptions
        {
            RootPath = "logs",
            DateFormat = "%"  // Invalid format specifier
        };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.Contains("Invalid DateFormat", exception.Message);
        Assert.Equal("DateFormat", exception.ParamName);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Fact]
    public void Validate_WithZeroMaxFileSize_ShouldThrowArgumentException()
    {
        var options = new FileLoggerOptions
        {
            RootPath = "logs",
            DateFormat = "yyyy-MM-dd",
            MaxFileSize = 0
        };

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.Contains("MaxFileSize must be greater than 0", exception.Message);
        Assert.Equal("MaxFileSize", exception.ParamName);
    }

    [Theory]
    [InlineData("yyyy-MM-dd")]
    [InlineData("yyyy-MM-dd HH:mm:ss")]
    [InlineData("yyyyMMdd")]
    [InlineData("yyyy/MM/dd")]
    public void Validate_WithValidDateFormats_ShouldNotThrow(string dateFormat)
    {
        var options = new FileLoggerOptions
        {
            RootPath = "logs",
            DateFormat = dateFormat
        };

        var exception = Record.Exception(() => options.Validate());

        Assert.Null(exception);
    }
}