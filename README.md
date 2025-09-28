# PolyLogger

一个灵活的文件日志库，完全兼容 Microsoft.Extensions.Logging。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0+-blue.svg)](https://dotnet.microsoft.com/)

## ✨ 功能特性

- 🔌 **完全兼容** Microsoft.Extensions.Logging
- 📁 **智能目录结构** 根据类型自动创建子目录
- 🔄 **文件滚动** 支持按日期和文件大小滚动
- 🏷️ **级别分离** 可按日志级别分文件存储
- ⚙️ **高度可配置** 支持自定义文件命名规则
- 🔒 **线程安全** 支持高并发场景
- 🚀 **依赖注入** 原生支持 .NET DI 容器

## 📦 安装

```bash
dotnet add package PolyLogger
```

## 🚀 快速开始

### 基本使用

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
            options.MinLogLevel = LogLevel.Information;
        });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Hello PolyLogger!");
```

### ASP.NET Core 集成

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFileLogger(options =>
{
    options.RootPath = "logs";
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();
```

## 📂 目录结构

启用目录结构后，日志文件会自动按类型组织：

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

## ⚙️ 主要配置选项

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `RootPath` | string | "logs" | 日志根目录 |
| `DateFormat` | string | "yyyy-MM-dd" | 日期格式 |
| `MaxFileSize` | long? | null | 文件大小限制 |
| `CreateCategoryDirectories` | bool | true | 创建目录结构 |
| `MinLogLevel` | LogLevel | Information | 最小日志级别 |
| `SeparateByLogLevel` | bool | false | 按级别分文件 |

## 📚 文档

详细文档请查看 [docs](./docs/) 目录：

- **[API 参考](./docs/api-reference.md)** - 完整的 API 文档
- **[配置指南](./docs/configuration-guide.md)** - 详细的配置说明
- **[使用示例](./docs/examples.md)** - 各种场景的使用示例
- **[故障排除](./docs/troubleshooting.md)** - 常见问题解决方案

## 💡 使用示例

### 文件大小滚动
```csharp
builder.AddFileLogger(options =>
{
    options.MaxFileSize = 1024 * 1024; // 1MB 自动滚动
});
```

### 按级别分文件
```csharp
builder.AddFileLogger(options =>
{
    options.SeparateByLogLevel = true;
});
// 生成: Error.log, Warning.log, Information.log
```

### 自定义命名规则
```csharp
builder.AddFileLogger(options =>
{
    options.FileNameRule = (category, level, time) =>
        $"{category}_{level}_{time:yyyyMMdd}.log";
});
```

## 📄 日志格式

```
[2025-09-28 14:30:25.123 +08:00] [INFORMATION] [MyApp.Services.UserService] 用户登录成功
[2025-09-28 14:30:26.456 +08:00] [WARNING] [MyApp.Controllers.ApiController] API 调用频率过高
[2025-09-28 14:30:27.789 +08:00] [ERROR] [MyApp.Services.OrderService] 订单处理失败
Exception: System.InvalidOperationException: 库存不足
   at MyApp.Services.OrderService.ProcessOrder(Int32 orderId)
```

## 🔧 系统要求

- .NET 9.0 或更高版本
- 支持 Windows、Linux、macOS

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

[MIT License](./LICENSE)