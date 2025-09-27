using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyLogger.Options;

namespace PolyLogger.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerOptions> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        builder.Services.Configure(configure);
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

        // Validate configuration on startup
        builder.Services.AddSingleton(provider =>
        {
            var options = new FileLoggerOptions();
            configure(options);
            options.Validate();
            return options;
        });

        return builder;
    }

    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        return builder;
    }

    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, FileLoggerOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.Validate();

        builder.Services.Configure<FileLoggerOptions>(opt =>
        {
            opt.RootPath = options.RootPath;
            opt.DateFormat = options.DateFormat;
            opt.MaxFileSize = options.MaxFileSize;
            opt.FileNameRule = options.FileNameRule;
            opt.CreateCategoryDirectories = options.CreateCategoryDirectories;
            opt.MinLogLevel = options.MinLogLevel;
            opt.SeparateByLogLevel = options.SeparateByLogLevel;
        });

        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

        return builder;
    }
}