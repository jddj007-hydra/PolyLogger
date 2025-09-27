using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolyLogger.Options;
using System.Text;
using System.Collections.Concurrent;

namespace PolyLogger;

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerOptions _options;
    private readonly object _lock = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    public FileLogger(string categoryName, IOptionsMonitor<FileLoggerOptions> options)
    {
        _categoryName = categoryName;
        _options = options.CurrentValue;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _options.MinLogLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        lock (_lock)
        {
            WriteLog(logLevel, eventId, message, exception);
        }
    }

    private void WriteLog(LogLevel logLevel, EventId eventId, string message, Exception? exception)
    {
        var filePath = GetLogFilePath(logLevel);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var logEntry = FormatLogEntry(logLevel, eventId, message, exception);

        // Check file size if MaxFileSize is set
        if (_options.MaxFileSize.HasValue && File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length >= _options.MaxFileSize.Value)
            {
                filePath = GetRolledFilePath(filePath);
            }
        }

        var semaphore = _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));

        try
        {
            semaphore.Wait();
            using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(logEntry);
            writer.Flush();
        }
        finally
        {
            semaphore.Release();
        }
    }

    private string GetLogFilePath(LogLevel logLevel)
    {
        var basePath = _options.RootPath;

        if (_options.CreateCategoryDirectories)
        {
            var categoryParts = _categoryName.Split('.', StringSplitOptions.RemoveEmptyEntries);
            basePath = Path.Combine(basePath, Path.Combine(categoryParts));
        }

        string fileName;
        var now = DateTimeOffset.Now;
        if (_options.FileNameRule != null)
        {
            fileName = _options.FileNameRule(_categoryName, logLevel, now.DateTime);
        }
        else if (_options.SeparateByLogLevel)
        {
            fileName = $"{logLevel}.log";
        }
        else
        {
            fileName = $"{now.ToString(_options.DateFormat)}.log";
        }

        return Path.Combine(basePath, fileName);
    }

    private string GetRolledFilePath(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        var counter = 1;
        const int maxRollAttempts = 1000;
        string newPath;

        do
        {
            newPath = Path.Combine(directory!, $"{fileNameWithoutExtension}_{counter:000}{extension}");
            counter++;

            if (counter > maxRollAttempts)
            {
                throw new InvalidOperationException($"Unable to create rolled file after {maxRollAttempts} attempts. Check disk space and permissions.");
            }
        }
        while (File.Exists(newPath) && new FileInfo(newPath).Length >= _options.MaxFileSize!.Value);

        return newPath;
    }

    private string FormatLogEntry(LogLevel logLevel, EventId eventId, string message, Exception? exception)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
        var level = logLevel.ToString().ToUpper();

        var sb = new StringBuilder();
        sb.AppendLine($"[{timestamp}] [{level}] [{_categoryName}] {message}");

        if (exception != null)
        {
            sb.AppendLine($"Exception: {exception}");
        }

        return sb.ToString();
    }
}