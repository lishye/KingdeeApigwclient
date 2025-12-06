# C# 版本

金蝶 API 网关客户端的 C# 实现。

## 文件说明

- **ApigwClient.cs** - 完整的 C# SDK 实现（390 行）
  - `ApigwConfig` - 配置类
  - `ApiRequest` - 请求类
  - `ApiResult` - 响应类
  - `ApigwClient` - 客户端类（单例）
  - `SignatureUtil` - 签名工具类
- **ApigwClientLib.cs** - 辅助类库
- **SignTest.cs** - 签名算法测试
- **TestApigwClient.cs** - API 调用测试
- **ApigwClient.csproj** - 项目文件
- **CSharpApiClient/** - 完整的 C# 项目目录

## 快速开始

### 编译
```bash
dotnet build
```

### 运行
```bash
dotnet run
```

## 需求

- .NET 9.0 或更高版本
- Visual Studio 2022 或 VS Code

## 功能

- ✅ App Signature 生成（HMAC-SHA256 → Hex → Base64）
- ✅ X-Api-Signature 请求签名
- ✅ 异步 API 请求发送和响应处理
- ✅ 错误处理和异常捕获
- ✅ 完整的日志输出

## 签名验证

程序包含内置的签名验证测试：
```
测试: app_key=abc, appSecret=abc123
验证结果: ✅ 通过
```

## 示例代码

```csharp
var config = new ApigwConfig {
    ClientId = "328301",
    ClientSecret = "1a97dac4f8c92a482424bf7732b115a1"
};

var client = ApigwClient.GetInstance();
client.SetConfig(config);

var request = new ApiRequest {
    Path = "/api/v1/access/token",
    Method = "POST"
};

var result = await client.SendAsync(request);
Console.WriteLine($"Status: {result.StatusCode}");
Console.WriteLine($"Body: {result.Body}");
```

## 输出示例

```
响应状态码: 200
响应内容: {"errcode":0,"description":"成功","data":{"access_token":"..."}}
```

## 类详解

### ApigwClient（单例）
```csharp
var client = ApigwClient.GetInstance();
client.SetConfig(config);
var result = await client.SendAsync(request);
```

### ApiRequest
```csharp
var request = new ApiRequest {
    Path = "/api/v1/user/get",
    Method = "POST"
};
request.AddQueryParam("key", "value");
request.AddHeader("X-Custom-Header", "value");
request.SetBody(new { data = "value" });
```

### ApiResult
```csharp
Console.WriteLine(result.StatusCode);  // HTTP 状态码
Console.WriteLine(result.Body);        // 响应体（JSON 字符串）
Console.WriteLine(result.Headers);     // 响应头
```

### SignatureUtil
```csharp
// App Signature
var appSig = SignatureUtil.GenerateAppSignature("client_id", "client_secret");

// X-Api-Signature
var sig = SignatureUtil.GetSignature(method, path, queryParams, headers);
```

## 特点

- **异步操作**：所有网络调用都是异步的，使用 `async/await`
- **现代 C#**：使用最新的 C# 语言特性（如 null-coalescing, string interpolation）
- **.NET 最佳实践**：遵循 Microsoft 的编码规范和最佳实践

## 构建项目

### 使用 Visual Studio
1. 打开 `Java.sln`
2. 在 Solution Explorer 中右键点击项目
3. 选择 "Build"

### 使用命令行
```bash
cd CSharp
dotnet build
dotnet run
```

## 调试

在 Visual Studio 中设置断点并按 F5 启动调试。

或使用命令行：
```bash
dotnet run --configuration Debug
```
