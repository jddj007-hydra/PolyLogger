# 故障排除指南

## 常见问题和解决方案

### 1. 日志文件未创建

**症状：** 配置了 PolyLogger 但没有生成日志文件

**可能原因和解决方案：**

#### 1.1 权限问题
```bash
# 检查目录权限
ls -la logs/

# 赋予写权限
chmod 755 logs/
# 或者在 Windows 中检查文件夹权限
```

**解决方案：**
```csharp
// 在代码中检查权限
try
{
    var testFile = Path.Combine("logs", "test.txt");
    Directory.CreateDirectory(Path.GetDirectoryName(testFile));
    File.WriteAllText(testFile, "test");
    File.Delete(testFile);
    Console.WriteLine("目录权限正常");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"权限错误: {ex.Message}");
}
```

#### 1.2 日志级别过高
```csharp
// 检查日志级别配置
builder.AddFileLogger(options =>
{
    options.MinLogLevel = LogLevel.Trace; // 降低最小级别
});

// 或者检查具体级别是否启用
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
Console.WriteLine($"Debug enabled: {logger.IsEnabled(LogLevel.Debug)}");
Console.WriteLine($"Info enabled: {logger.IsEnabled(LogLevel.Information)}");
```

#### 1.3 配置错误
```csharp
// 验证配置
try
{
    var options = new FileLoggerOptions
    {
        RootPath = "logs",
        DateFormat = "yyyy-MM-dd"
    };
    options.Validate(); // 手动验证配置
}
catch (ArgumentException ex)
{
    Console.WriteLine($"配置错误: {ex.Message}");
}
```

### 2. 文件写入性能问题

**症状：** 应用程序在写入日志时变慢

**诊断和解决：**

#### 2.1 减少日志量
```csharp
// 提高最小日志级别
builder.AddFileLogger(options =>
{
    options.MinLogLevel = LogLevel.Warning; // 只记录警告及以上
});

// 条件日志记录
if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug("昂贵的调试信息: {Data}", ExpensiveOperation());
}
```

#### 2.2 优化文件大小
```csharp
// 设置文件大小限制，避免单个文件过大
builder.AddFileLogger(options =>
{
    options.MaxFileSize = 5 * 1024 * 1024; // 5MB
});
```

#### 2.3 简化目录结构
```csharp
// 禁用目录结构以减少目录创建开销
builder.AddFileLogger(options =>
{
    options.CreateCategoryDirectories = false;
});
```

### 3. 磁盘空间问题

**症状：** 应用程序因磁盘空间不足而崩溃

**监控和解决：**

#### 3.1 实现日志清理
```csharp
public class LogCleanupService : BackgroundService
{
    private readonly ILogger<LogCleanupService> _logger;
    private readonly string _logPath;

    public LogCleanupService(ILogger<LogCleanupService> logger)
    {
        _logger = logger;
        _logPath = "logs";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CleanupOldLogs();
                CheckDiskSpace();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志清理服务出错");
            }
        }
    }

    private void CleanupOldLogs()
    {
        var cutoffDate = DateTime.Now.AddDays(-7);
        var files = Directory.GetFiles(_logPath, "*.log", SearchOption.AllDirectories)
            .Where(f => File.GetCreationTime(f) < cutoffDate);

        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
                _logger.LogInformation("删除旧日志文件: {File}", file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法删除日志文件: {File}", file);
            }
        }
    }

    private void CheckDiskSpace()
    {
        var drive = new DriveInfo(Path.GetPathRoot(_logPath));
        var freeSpaceGB = drive.TotalFreeSpace / (1024.0 * 1024 * 1024);

        if (freeSpaceGB < 1.0) // 小于1GB警告
        {
            _logger.LogWarning("磁盘空间不足: {FreeSpace:F2} GB", freeSpaceGB);
        }
    }
}
```

#### 3.2 实现磁盘空间检查
```csharp
public class DiskSpaceChecker
{
    public static bool HasEnoughSpace(string path, long requiredBytes)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path)));
            return drive.TotalFreeSpace >= requiredBytes;
        }
        catch
        {
            return false;
        }
    }
}

// 在写入前检查
if (!DiskSpaceChecker.HasEnoughSpace("logs", 100 * 1024 * 1024)) // 100MB
{
    logger.LogError("磁盘空间不足，停止日志记录");
    return;
}
```

### 4. 文件锁定问题

**症状：** 其他程序无法访问日志文件，或出现文件访问异常

**解决方案：**

#### 4.1 确保正确的文件共享
文件已通过 `FileShare.Read` 配置，允许其他程序读取：

```csharp
// 在 FileLogger.cs 中已实现
using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
```

#### 4.2 处理文件访问异常
```csharp
public class RobustFileLogger
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 100;

    public async Task WriteLogAsync(string filePath, string content)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                await File.AppendAllTextAsync(filePath, content);
                return;
            }
            catch (IOException ex) when (attempt < MaxRetries - 1)
            {
                await Task.Delay(RetryDelayMs * (attempt + 1));
            }
        }
    }
}
```

### 5. 字符编码问题

**症状：** 日志文件中出现乱码

**解决方案：**

```csharp
// 确保使用 UTF-8 编码（已在 FileLogger 中实现）
using var writer = new StreamWriter(stream, Encoding.UTF8);
```

如果需要其他编码：
```csharp
// 自定义 FileLogger 以支持不同编码
public class CustomFileLogger : FileLogger
{
    private readonly Encoding _encoding;

    public CustomFileLogger(string categoryName, IOptionsMonitor<FileLoggerOptions> options, Encoding encoding)
        : base(categoryName, options)
    {
        _encoding = encoding;
    }

    // 重写写入方法以使用自定义编码
}
```

### 6. 时区问题

**症状：** 日志时间戳不正确

**解决方案：**

```csharp
// 检查系统时区
Console.WriteLine($"系统时区: {TimeZoneInfo.Local.DisplayName}");
Console.WriteLine($"当前时间: {DateTimeOffset.Now}");

// 如果需要 UTC 时间
public class UtcFileLogger : FileLogger
{
    protected override string FormatLogEntry(LogLevel logLevel, EventId eventId, string message, Exception? exception)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC");
        // ... 格式化逻辑
    }
}
```

### 7. 配置不生效

**症状：** 修改配置后没有看到预期的行为

**诊断步骤：**

#### 7.1 验证配置加载
```csharp
// 添加配置调试
services.Configure<FileLoggerOptions>(options =>
{
    Console.WriteLine($"配置加载 - RootPath: {options.RootPath}");
    Console.WriteLine($"配置加载 - MinLogLevel: {options.MinLogLevel}");
});
```

#### 7.2 检查配置绑定
```csharp
// 验证配置文件绑定
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var fileLoggerSection = configuration.GetSection("Logging:FileLogger");
if (!fileLoggerSection.Exists())
{
    Console.WriteLine("FileLogger 配置节不存在");
}

var options = fileLoggerSection.Get<FileLoggerOptions>();
Console.WriteLine($"从配置文件读取的 RootPath: {options?.RootPath}");
```

#### 7.3 配置优先级
```csharp
// 检查配置源优先级
var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")           // 低优先级
    .AddJsonFile("appsettings.Production.json", optional: true) // 中优先级
    .AddEnvironmentVariables()                 // 高优先级
    .AddCommandLine(args);                     // 最高优先级

var config = builder.Build();
```

### 8. 内存泄漏

**症状：** 应用程序内存使用量持续增长

**诊断和解决：**

#### 8.1 正确释放资源
```csharp
// 确保 ServiceProvider 被正确释放
using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
// serviceProvider 会在 using 块结束时自动释放
```

#### 8.2 检查日志器缓存
```csharp
// FileLoggerProvider 已实现了日志器缓存
// 如果需要清理，可以手动调用 Dispose
public void ClearLoggers()
{
    if (_loggerProvider is IDisposable disposable)
    {
        disposable.Dispose();
    }
}
```

### 9. 并发问题

**症状：** 在高并发场景下出现日志丢失或异常

**解决方案：**

PolyLogger 已实现线程安全，但如果仍有问题：

```csharp
// 检查并发写入
public class ConcurrencyTestService
{
    private readonly ILogger<ConcurrencyTestService> _logger;

    public async Task TestConcurrentLogging()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    _logger.LogInformation("线程 {ThreadId} 消息 {MessageId}",
                        Thread.CurrentThread.ManagedThreadId, j);
                }
            }));

        await Task.WhenAll(tasks);
    }
}
```

### 10. 调试技巧

#### 10.1 启用详细日志
```csharp
// 临时启用所有级别的日志进行调试
builder.AddFileLogger(options =>
{
    options.MinLogLevel = LogLevel.Trace;
    options.RootPath = "debug-logs";
});

// 添加控制台日志进行对比
builder.AddConsole();
```

#### 10.2 日志输出验证
```csharp
public class LoggingDiagnostics
{
    public static void DiagnoseLogging(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<LoggingDiagnostics>>();

        // 测试各个级别
        logger.LogTrace("Trace 级别测试");
        logger.LogDebug("Debug 级别测试");
        logger.LogInformation("Information 级别测试");
        logger.LogWarning("Warning 级别测试");
        logger.LogError("Error 级别测试");
        logger.LogCritical("Critical 级别测试");

        // 检查级别启用状态
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            if (level != LogLevel.None)
            {
                Console.WriteLine($"{level}: {logger.IsEnabled(level)}");
            }
        }
    }
}
```

#### 10.3 文件系统监控
```csharp
public class FileSystemWatcher
{
    public static void MonitorLogDirectory(string path)
    {
        var watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
            Filter = "*.log"
        };

        watcher.Created += (s, e) => Console.WriteLine($"文件创建: {e.Name}");
        watcher.Changed += (s, e) => Console.WriteLine($"文件修改: {e.Name}");

        watcher.EnableRaisingEvents = true;
    }
}
```

## 获得帮助

如果以上解决方案无法解决问题，请：

1. **检查版本兼容性**：确保使用的是最新版本的 PolyLogger
2. **查看 GitHub Issues**：搜索已知问题和解决方案
3. **提供详细信息**：包括配置、异常信息、环境详情
4. **创建最小重现案例**：提供能重现问题的最小代码示例

### 问题报告模板

```
**环境信息：**
- PolyLogger 版本：
- .NET 版本：
- 操作系统：
- 应用程序类型：（控制台/ASP.NET Core/Worker Service）

**配置：**
```csharp
// 贴出相关配置代码
```

**预期行为：**
// 描述期望的行为

**实际行为：**
// 描述实际发生的情况

**重现步骤：**
1.
2.
3.

**额外信息：**
// 任何其他相关信息、错误消息、日志等
```