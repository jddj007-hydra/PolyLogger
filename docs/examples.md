# 使用示例

## 基础示例

### 1. 控制台应用程序

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

// 创建主机
var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(builder =>
    {
        builder.ClearProviders(); // 清除默认提供器
        builder.AddFileLogger(options =>
        {
            options.RootPath = "logs";
            options.MinLogLevel = LogLevel.Information;
        });
    })
    .Build();

// 获取日志器
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// 记录日志
logger.LogInformation("应用程序启动");
logger.LogWarning("这是一个警告消息");
logger.LogError("这是一个错误消息");

// 启动主机
await host.RunAsync();
```

### 2. ASP.NET Core 应用程序

```csharp
using PolyLogger.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 配置文件日志
builder.Logging.AddFileLogger(options =>
{
    options.RootPath = "logs";
    options.DateFormat = "yyyy-MM-dd";
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
    options.CreateCategoryDirectories = true;
});

var app = builder.Build();

// 使用日志
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("收到根路径请求");
    return "Hello World!";
});

app.Run();
```

### 3. Worker Service

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

// 创建 Worker Service
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(builder =>
    {
        builder.AddFileLogger(options =>
        {
            options.RootPath = "/var/log/worker";
            options.SeparateByLogLevel = true;
        });
    })
    .Build();

await host.RunAsync();

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

## 高级示例

### 4. 自定义文件命名规则

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.RootPath = "logs";

        // 自定义命名规则：按服务和级别分文件
        options.FileNameRule = (categoryName, logLevel, dateTime) =>
        {
            // 从完整类名提取服务名
            var serviceName = categoryName.Split('.').LastOrDefault() ?? "Unknown";

            // 格式：ServiceName_Level_Date.log
            return $"{serviceName}_{logLevel}_{dateTime:yyyy-MM-dd}.log";
        };
    });
});

var serviceProvider = services.BuildServiceProvider();

// 不同的服务会产生不同的文件
var userLogger = serviceProvider.GetRequiredService<ILogger<UserService>>();
var orderLogger = serviceProvider.GetRequiredService<ILogger<OrderService>>();

userLogger.LogInformation("用户操作");    // -> UserService_Information_2025-09-28.log
orderLogger.LogError("订单错误");        // -> OrderService_Error_2025-09-28.log

public class UserService { }
public class OrderService { }
```

### 5. 配置文件集成

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "FileLogger": {
      "RootPath": "logs",
      "DateFormat": "yyyy-MM-dd",
      "MaxFileSize": 5242880,
      "CreateCategoryDirectories": true,
      "MinLogLevel": "Information",
      "SeparateByLogLevel": false
    }
  }
}
```

**Program.cs:**
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;
using PolyLogger.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((context, builder) =>
    {
        // 绑定配置文件
        builder.Services.Configure<FileLoggerOptions>(
            context.Configuration.GetSection("Logging:FileLogger"));

        builder.AddFileLogger();
    })
    .Build();

await host.RunAsync();
```

### 6. 多环境配置

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((context, builder) =>
    {
        var environment = context.HostingEnvironment;

        if (environment.IsDevelopment())
        {
            // 开发环境：详细日志，按小时分文件
            builder.AddFileLogger(options =>
            {
                options.RootPath = "logs/dev";
                options.MinLogLevel = LogLevel.Debug;
                options.DateFormat = "yyyy-MM-dd_HH";
                options.CreateCategoryDirectories = true;
            });
        }
        else if (environment.IsProduction())
        {
            // 生产环境：精简日志，按级别分文件
            builder.AddFileLogger(options =>
            {
                options.RootPath = "/var/log/myapp";
                options.MinLogLevel = LogLevel.Information;
                options.MaxFileSize = 50 * 1024 * 1024; // 50MB
                options.SeparateByLogLevel = true;
            });
        }
    })
    .Build();
```

### 7. 微服务场景

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

var services = new ServiceCollection();

// 获取服务标识
var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "Unknown";
var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? Guid.NewGuid().ToString()[..8];

services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.RootPath = $"/var/log/{serviceName}";

        // 包含实例ID的文件命名
        options.FileNameRule = (category, level, time) =>
        {
            return $"{serviceName}_{instanceId}_{time:yyyy-MM-dd}.log";
        };

        options.MaxFileSize = 20 * 1024 * 1024; // 20MB
    });
});

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("微服务实例 {ServiceName}:{InstanceId} 启动", serviceName, instanceId);
```

### 8. 结构化日志

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.RootPath = "logs";
        options.CreateCategoryDirectories = true;
    });
});

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<UserService>>();

// 结构化日志记录
logger.LogInformation("用户 {UserId} 执行了 {Action} 操作", 12345, "登录");
logger.LogWarning("用户 {UserId} 登录失败，尝试次数: {AttemptCount}", 12345, 3);

// 使用作用域
using (logger.BeginScope("RequestId: {RequestId}", Guid.NewGuid()))
{
    logger.LogInformation("开始处理请求");
    logger.LogInformation("请求处理完成");
}

// 异常日志
try
{
    throw new InvalidOperationException("模拟异常");
}
catch (Exception ex)
{
    logger.LogError(ex, "处理用户请求时发生错误，用户ID: {UserId}", 12345);
}

public class UserService { }
```

### 9. 性能敏感场景

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.RootPath = "logs";
        options.MinLogLevel = LogLevel.Warning; // 只记录警告及以上
        options.CreateCategoryDirectories = false; // 减少目录操作
        options.MaxFileSize = 5 * 1024 * 1024; // 5MB快速滚动
    });
});

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<HighPerformanceService>>();

// 条件日志记录
if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug("调试信息: {Data}", ExpensiveOperation());
}

// 高频操作中的日志记录
for (int i = 0; i < 1000; i++)
{
    try
    {
        ProcessItem(i);
    }
    catch (Exception ex)
    {
        // 只记录异常，避免大量正常日志
        logger.LogError(ex, "处理项目 {ItemId} 时发生错误", i);
    }
}

string ExpensiveOperation() => "昂贵的操作结果";
void ProcessItem(int id) { /* 处理逻辑 */ }

public class HighPerformanceService { }
```

### 10. 日志轮换和清理

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<LogCleanupService>();
    })
    .ConfigureLogging(builder =>
    {
        builder.AddFileLogger(options =>
        {
            options.RootPath = "logs";
            options.MaxFileSize = 10 * 1024 * 1024; // 10MB自动滚动
        });
    })
    .Build();

await host.RunAsync();

// 日志清理服务
public class LogCleanupService : BackgroundService
{
    private readonly ILogger<LogCleanupService> _logger;
    private readonly string _logDirectory = "logs";

    public LogCleanupService(ILogger<LogCleanupService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CleanupOldLogs();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // 每小时清理一次
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志清理过程中发生错误");
            }
        }
    }

    private void CleanupOldLogs()
    {
        if (!Directory.Exists(_logDirectory))
            return;

        var cutoffDate = DateTime.Now.AddDays(-7); // 保留7天的日志

        var logFiles = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories)
            .Where(file => File.GetCreationTime(file) < cutoffDate);

        foreach (var file in logFiles)
        {
            try
            {
                File.Delete(file);
                _logger.LogInformation("删除旧日志文件: {FileName}", file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法删除日志文件: {FileName}", file);
            }
        }
    }
}
```

## Docker 集成示例

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY . .

# 创建日志目录
RUN mkdir -p /app/logs
VOLUME ["/app/logs"]

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'
services:
  myapp:
    build: .
    volumes:
      - ./logs:/app/logs
    environment:
      - Logging__FileLogger__RootPath=/app/logs
      - Logging__FileLogger__MaxFileSize=10485760
    ports:
      - "5000:5000"
```

这些示例涵盖了 PolyLogger 在各种场景下的使用方法，从简单的控制台应用到复杂的微服务架构。根据具体需求选择合适的配置和使用模式。