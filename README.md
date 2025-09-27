# PolyLogger

一个灵活的文件日志库，兼容 Microsoft.Extensions.Logging。

## 功能特性

- ✅ 实现 `ILogger` 和 `ILogger<T>`，完全兼容 Microsoft.Extensions.Logging
- ✅ 支持依赖注入 (DI) 注册
- ✅ 根据类型自动创建子目录结构
- ✅ 支持按日期滚动（默认 yyyy-MM-dd.log）
- ✅ 支持按文件大小滚动
- ✅ 支持按日志级别分文件
- ✅ 可自定义文件命名规则
- ✅ 线程安全

## 安装

```bash
dotnet add package PolyLogger
```

## 基本使用

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolyLogger.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(builder =>
    {
        builder.AddFileLogger(options =>
        {
            options.RootPath = "logs";
            options.DateFormat = "yyyy-MM-dd";
            options.CreateCategoryDirectories = true;
            options.MinLogLevel = LogLevel.Information;
        });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Hello PolyLogger!");
```

## 目录结构

使用 `ILogger<T>` 时，会根据类型自动创建目录结构：

```
logs/
├── Services/
│   ├── UserService/
│   │   └── 2025-09-26.log
│   └── OrderService/
│       └── 2025-09-26.log
└── Controllers/
    └── HomeController/
        └── 2025-09-26.log
```

## 配置选项

### FileLoggerOptions

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `RootPath` | string | "logs" | 日志根目录 |
| `DateFormat` | string | "yyyy-MM-dd" | 日期格式 |
| `MaxFileSize` | long? | null | 单文件最大大小（字节） |
| `FileNameRule` | Func | null | 自定义文件命名函数 |
| `CreateCategoryDirectories` | bool | true | 是否创建类型目录 |
| `MinLogLevel` | LogLevel | Information | 最小日志级别 |
| `SeparateByLogLevel` | bool | false | 是否按级别分文件 |

## 高级配置

### 按文件大小滚动

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = "logs";
    options.MaxFileSize = 1024 * 1024; // 1MB
});
```

### 按日志级别分文件

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = "logs";
    options.SeparateByLogLevel = true;
});
```

生成文件：
- `Error.log`
- `Information.log`
- `Warning.log`

### 自定义文件命名规则

```csharp
builder.AddFileLogger(options =>
{
    options.RootPath = "logs";
    options.FileNameRule = (categoryName, logLevel, dateTime) =>
    {
        return $"{categoryName}_{logLevel}_{dateTime:yyyyMMdd_HHmmss}.log";
    };
});
```

## 日志格式

默认日志格式：
```
[2025-09-26 23:03:16.059] [INFORMATION] [MyApp.Services.UserService] 用户创建成功
```

## 许可证

MIT License