# API 参考文档

## 概述

PolyLogger 提供了一套完整的文件日志API，完全兼容 Microsoft.Extensions.Logging 框架。

## 核心类型

### FileLogger

主要的日志记录器类，实现了 `ILogger` 接口。

```csharp
public class FileLogger : ILogger
```

#### 方法

##### Log<TState>
```csharp
public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
```

记录日志消息。

**参数：**
- `logLevel`: 日志级别
- `eventId`: 事件ID
- `state`: 状态对象
- `exception`: 异常对象（可选）
- `formatter`: 格式化函数

##### IsEnabled
```csharp
public bool IsEnabled(LogLevel logLevel)
```

检查指定的日志级别是否启用。

**参数：**
- `logLevel`: 要检查的日志级别

**返回值：** 如果启用返回 `true`，否则返回 `false`

##### BeginScope<TState>
```csharp
public IDisposable? BeginScope<TState>(TState state) where TState : notnull
```

开始一个日志作用域（当前实现返回 `null`）。

### FileLoggerProvider

日志提供器类，实现了 `ILoggerProvider` 接口。

```csharp
public class FileLoggerProvider : ILoggerProvider
```

#### 构造函数

```csharp
public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
```

**参数：**
- `options`: 文件日志配置选项监视器

#### 方法

##### CreateLogger
```csharp
public ILogger CreateLogger(string categoryName)
```

创建指定类别的日志记录器。

**参数：**
- `categoryName`: 日志类别名称

**返回值：** `ILogger` 实例

##### Dispose
```csharp
public void Dispose()
```

释放资源。

### FileLoggerOptions

配置选项类，用于配置文件日志的行为。

```csharp
public class FileLoggerOptions
```

#### 属性

##### RootPath
```csharp
public string RootPath { get; set; } = "logs";
```

日志文件的根目录路径。默认值为 "logs"。

##### DateFormat
```csharp
public string DateFormat { get; set; } = "yyyy-MM-dd";
```

日期格式字符串，用于生成日志文件名。默认值为 "yyyy-MM-dd"。

##### MaxFileSize
```csharp
public long? MaxFileSize { get; set; }
```

单个日志文件的最大大小（字节）。如果为 `null`，则不限制文件大小。

##### FileNameRule
```csharp
public Func<string, LogLevel, DateTime, string>? FileNameRule { get; set; }
```

自定义文件命名规则函数。如果设置，将覆盖默认的命名逻辑。

**函数参数：**
- `string`: 类别名称
- `LogLevel`: 日志级别
- `DateTime`: 当前时间
- **返回值：** 文件名（不包含路径）

##### CreateCategoryDirectories
```csharp
public bool CreateCategoryDirectories { get; set; } = true;
```

是否根据日志类别创建目录结构。默认值为 `true`。

##### MinLogLevel
```csharp
public LogLevel MinLogLevel { get; set; } = LogLevel.Information;
```

最小日志级别。低于此级别的日志将被忽略。默认值为 `LogLevel.Information`。

##### SeparateByLogLevel
```csharp
public bool SeparateByLogLevel { get; set; } = false;
```

是否按日志级别分别创建文件。默认值为 `false`。

#### 方法

##### Validate
```csharp
public void Validate()
```

验证配置选项的有效性。如果配置无效，将抛出 `ArgumentException`。

**验证规则：**
- `RootPath` 不能为空或仅包含空白字符
- `DateFormat` 不能为空或仅包含空白字符，且必须是有效的日期格式
- `MaxFileSize` 如果设置，必须大于 0

## 扩展方法

### LoggingBuilderExtensions

提供便捷的扩展方法来注册文件日志服务。

```csharp
public static class LoggingBuilderExtensions
```

#### AddFileLogger (带配置委托)
```csharp
public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerOptions> configure)
```

添加文件日志服务并配置选项。

**参数：**
- `builder`: 日志构建器
- `configure`: 配置委托

**返回值：** `ILoggingBuilder` 实例

**示例：**
```csharp
services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.RootPath = "logs";
        options.MaxFileSize = 1024 * 1024; // 1MB
    });
});
```

#### AddFileLogger (无参数)
```csharp
public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
```

使用默认配置添加文件日志服务。

**参数：**
- `builder`: 日志构建器

**返回值：** `ILoggingBuilder` 实例

#### AddFileLogger (带选项对象)
```csharp
public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, FileLoggerOptions options)
```

使用预配置的选项对象添加文件日志服务。

**参数：**
- `builder`: 日志构建器
- `options`: 预配置的选项对象

**返回值：** `ILoggingBuilder` 实例

## 异常

### ArgumentException

当配置验证失败时抛出：

- **RootPath 验证失败**：`ArgumentException("RootPath cannot be null or empty.", nameof(RootPath))`
- **DateFormat 验证失败**：`ArgumentException("DateFormat cannot be null or empty.", nameof(DateFormat))`
- **MaxFileSize 验证失败**：`ArgumentException("MaxFileSize must be greater than 0.", nameof(MaxFileSize))`
- **DateFormat 格式无效**：`ArgumentException($"Invalid DateFormat: {DateFormat}", nameof(DateFormat), FormatException)`

### InvalidOperationException

当文件滚动操作失败时抛出：

- **文件滚动失败**：`InvalidOperationException($"Unable to create rolled file after {maxRollAttempts} attempts. Check disk space and permissions.")`

## 线程安全

PolyLogger 是线程安全的：

- 每个 `FileLogger` 实例使用内部锁来保护写入操作
- 使用 `SemaphoreSlim` 来控制对同一文件的并发访问
- 多个线程可以安全地同时写入不同的日志文件

## 性能考虑

- 日志写入是同步操作
- 文件 I/O 操作会阻塞调用线程
- 建议在高频日志场景中配置适当的最小日志级别
- 大文件会影响滚动性能，建议设置合理的 `MaxFileSize`

## 最佳实践

1. **配置验证**：在应用启动时验证配置
2. **目录权限**：确保应用对日志目录有写入权限
3. **磁盘空间**：监控磁盘空间，避免日志填满磁盘
4. **文件滚动**：在高频日志场景中使用文件大小滚动
5. **日志级别**：合理设置最小日志级别，避免过多的调试日志