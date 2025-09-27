using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolyLogger.Options;
using System.Collections.Concurrent;

namespace PolyLogger;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly IOptionsMonitor<FileLoggerOptions> _options;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private bool _disposed = false;

    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
    {
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _loggers.Clear();
            _disposed = true;
        }
    }
}