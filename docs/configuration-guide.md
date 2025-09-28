# 配置指南

## 概述

PolyLogger 提供了灵活的配置选项，可以满足不同场景下的日志记录需求。本指南将详细介绍各种配置方式和最佳实践。

## 基本配置

### 默认配置

最简单的配置方式，使用所有默认设置：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

services.AddLogging(builder =>
{
    builder.AddFileLogger(); // 使用默认配置
});
```

**默认设置：**
- `RootPath`: "logs"
- `DateFormat`: "yyyy-MM-dd"
- `MaxFileSize`: null (无限制)
- `CreateCategoryDirectories`: true
- `MinLogLevel`: LogLevel.Information
- `SeparateByLogLevel`: false

### 自定义配置

```csharp
services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.RootPath = "mylogs";
        options.DateFormat = "yyyyMMdd";
        options.MinLogLevel = LogLevel.Debug;
        options.CreateCategoryDirectories = false;
    });
});
```

## 详细配置选项

### 1. 根目录配置 (RootPath)

指定日志文件的根目录：

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = "/var/log/myapp";        // 绝对路径
    options.RootPath = "logs";                  // 相对路径
    options.RootPath = @"C:\Logs\MyApp";        // Windows路径
});
```

**注意事项：**
- 确保应用程序对指定目录有写入权限
- 目录不存在时会自动创建
- 相对路径相对于应用程序的工作目录

### 2. 日期格式配置 (DateFormat)

控制日志文件名中的日期部分：

```csharp
builder.AddFileLogger(options =>
{
    options.DateFormat = "yyyy-MM-dd";          // 2025-09-28.log
    options.DateFormat = "yyyyMMdd";            // 20250928.log
    options.DateFormat = "yyyy-MM-dd_HH";       // 2025-09-28_14.log (按小时)
    options.DateFormat = "yyyy/MM/dd";          // 包含路径分隔符（不推荐）
});
```

**支持的格式：**
- 标准 .NET 日期时间格式字符串
- 自定义格式组合

**注意事项：**
- 避免在格式中使用文件系统不支持的字符
- 格式验证在启动时进行

### 3. 文件大小限制 (MaxFileSize)

控制单个日志文件的最大大小：

```csharp
builder.AddFileLogger(options =>
{
    options.MaxFileSize = 1024 * 1024;          // 1MB
    options.MaxFileSize = 10 * 1024 * 1024;     // 10MB
    options.MaxFileSize = 1024L * 1024 * 1024;  // 1GB
    options.MaxFileSize = null;                 // 无限制（默认）
});
```

**文件滚动规则：**
- 当文件大小达到限制时，创建新文件
- 新文件名格式：`原文件名_001.log`、`原文件名_002.log` 等
- 最多尝试1000次滚动

### 4. 目录结构配置 (CreateCategoryDirectories)

控制是否根据日志类别创建目录结构：

```csharp
// 启用目录结构（默认）
builder.AddFileLogger(options =>
{
    options.CreateCategoryDirectories = true;
});
```

**目录结构示例：**
```
logs/
├── MyApp/
│   ├── Controllers/
│   │   ├── HomeController/
│   │   │   └── 2025-09-28.log
│   │   └── ApiController/
│   │       └── 2025-09-28.log
│   └── Services/
│       └── UserService/
│           └── 2025-09-28.log
```

```csharp
// 禁用目录结构
builder.AddFileLogger(options =>
{
    options.CreateCategoryDirectories = false;
});
```

**扁平结构示例：**
```
logs/
├── 2025-09-28.log  (所有日志混合)
```

### 5. 最小日志级别 (MinLogLevel)

控制记录哪些级别的日志：

```csharp
builder.AddFileLogger(options =>
{
    options.MinLogLevel = LogLevel.Trace;       // 记录所有级别
    options.MinLogLevel = LogLevel.Debug;       // 调试级别及以上
    options.MinLogLevel = LogLevel.Information; // 信息级别及以上（默认）
    options.MinLogLevel = LogLevel.Warning;     // 警告级别及以上
    options.MinLogLevel = LogLevel.Error;       // 错误级别及以上
    options.MinLogLevel = LogLevel.Critical;    // 仅关键错误
});
```

### 6. 按级别分文件 (SeparateByLogLevel)

是否按日志级别创建不同的文件：

```csharp
builder.AddFileLogger(options =>
{
    options.SeparateByLogLevel = true;
});
```

**生成的文件结构：**
```
logs/
├── Error.log
├── Warning.log
├── Information.log
├── Debug.log
└── Trace.log
```

### 7. 自定义文件命名规则 (FileNameRule)

完全自定义文件命名逻辑：

```csharp
builder.AddFileLogger(options =>
{
    options.FileNameRule = (categoryName, logLevel, dateTime) =>
    {
        // 包含类别和级别的文件名
        return $"{categoryName}_{logLevel}_{dateTime:yyyyMMdd}.log";
    };
});
```

**函数参数：**
- `categoryName`: 日志类别名称（如 "MyApp.Services.UserService"）
- `logLevel`: 当前日志级别
- `dateTime`: 当前时间

**示例命名规则：**

```csharp
// 按小时分文件
options.FileNameRule = (category, level, time) =>
    $"{time:yyyy-MM-dd-HH}.log";

// 包含进程ID
options.FileNameRule = (category, level, time) =>
    $"{time:yyyy-MM-dd}_pid{Environment.ProcessId}.log";

// 简化的类别名称
options.FileNameRule = (category, level, time) =>
{
    var simpleName = category.Split('.').LastOrDefault() ?? "Unknown";
    return $"{simpleName}_{time:yyyyMMdd}.log";
};
```

## 配置方式

### 1. 代码配置

直接在代码中配置：

```csharp
services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.RootPath = "logs";
        options.MaxFileSize = 1024 * 1024;
    });
});
```

### 2. 配置文件

通过 `appsettings.json` 配置：

```json
{
  "Logging": {
    "FileLogger": {
      "RootPath": "logs",
      "DateFormat": "yyyy-MM-dd",
      "MaxFileSize": 1048576,
      "CreateCategoryDirectories": true,
      "MinLogLevel": "Information",
      "SeparateByLogLevel": false
    }
  }
}
```

然后在代码中绑定：

```csharp
services.Configure<FileLoggerOptions>(
    configuration.GetSection("Logging:FileLogger"));

services.AddLogging(builder =>
{
    builder.AddFileLogger();
});
```

### 3. 环境变量

通过环境变量配置：

```bash
export Logging__FileLogger__RootPath="/var/log/myapp"
export Logging__FileLogger__MaxFileSize="1048576"
```

### 4. 命令行参数

通过命令行参数配置：

```bash
dotnet run --Logging:FileLogger:RootPath="logs" --Logging:FileLogger:MaxFileSize="1048576"
```

## 场景配置示例

### 开发环境

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = "logs";
    options.MinLogLevel = LogLevel.Debug;
    options.CreateCategoryDirectories = true;
    options.DateFormat = "yyyy-MM-dd_HH";  // 按小时分文件，便于调试
});
```

### 生产环境

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = "/var/log/myapp";
    options.MinLogLevel = LogLevel.Information;
    options.MaxFileSize = 10 * 1024 * 1024;  // 10MB滚动
    options.CreateCategoryDirectories = true;
    options.SeparateByLogLevel = true;       // 按级别分文件
});
```

### 高频日志场景

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = "logs";
    options.MinLogLevel = LogLevel.Warning;   // 只记录警告及以上
    options.MaxFileSize = 5 * 1024 * 1024;   // 5MB快速滚动
    options.CreateCategoryDirectories = false; // 减少目录创建开销
});
```

### 微服务环境

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = $"/var/log/{Environment.GetEnvironmentVariable("SERVICE_NAME")}";
    options.FileNameRule = (category, level, time) =>
        $"{Environment.MachineName}_{time:yyyy-MM-dd}.log";
    options.MaxFileSize = 50 * 1024 * 1024;  // 50MB
});
```

## 配置验证

PolyLogger 会在启动时验证配置：

```csharp
try
{
    services.AddLogging(builder =>
    {
        builder.AddFileLogger(options =>
        {
            options.RootPath = "";  // 无效：空路径
            options.DateFormat = "%"; // 无效：格式错误
            options.MaxFileSize = -1; // 无效：负数
        });
    });
}
catch (ArgumentException ex)
{
    // 处理配置错误
    Console.WriteLine($"配置错误: {ex.Message}");
}
```

## 最佳实践

1. **权限管理**：确保日志目录有适当的写入权限
2. **磁盘空间**：定期清理旧日志文件，避免磁盘空间不足
3. **性能考虑**：在高频场景中适当提高最小日志级别
4. **文件大小**：设置合理的文件大小限制，避免单个文件过大
5. **目录结构**：根据应用复杂度决定是否启用目录结构
6. **监控**：监控日志文件大小和磁盘使用情况
7. **备份**：制定日志文件的备份和归档策略