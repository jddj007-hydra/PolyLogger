using Microsoft.Extensions.Logging;

namespace PolyLogger.Options;

public class FileLoggerOptions
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RootPath))
            throw new ArgumentException("RootPath cannot be null or empty.", nameof(RootPath));

        if (string.IsNullOrWhiteSpace(DateFormat))
            throw new ArgumentException("DateFormat cannot be null or empty.", nameof(DateFormat));

        if (MaxFileSize.HasValue && MaxFileSize.Value <= 0)
            throw new ArgumentException("MaxFileSize must be greater than 0.", nameof(MaxFileSize));

        try
        {
            var testDate = DateTimeOffset.Now.ToString(DateFormat);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"Invalid DateFormat: {DateFormat}", nameof(DateFormat), ex);
        }
    }
    public string RootPath { get; set; } = "logs";

    public string DateFormat { get; set; } = "yyyy-MM-dd";

    public long? MaxFileSize { get; set; }

    public Func<string, LogLevel, DateTime, string>? FileNameRule { get; set; }

    public bool CreateCategoryDirectories { get; set; } = true;

    public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

    public bool SeparateByLogLevel { get; set; } = false;
}