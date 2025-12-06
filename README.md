# 金蝶 apigwclient API 网关客户端 SDK

一个完整的 API 网关客户端库实现，支持 Java、C# 和 Python 三种语言。用于与金蝶(Kingdee) API 网关进行交互，支持认证、请求签名和响应处理。
金蝶官方只有Java版本能够测试连接成功,且文档中缺少签名generateSignature的方法示例.
C#版本和Python版本使用官方的版本都是 API 返回了业务错误："授权密钥校验失败，请获取最新密钥加密".
使用官方的签名算法描述(#认证机制),重新实现了C#和Python算法.

## 📁 项目结构

```
.
├── Java/                      # Java 版本实现
│   ├── TestApp.java          # 测试应用程序
│   ├── TestApp.class         # 编译后的字节码
│   ├── apigwclient-0.1.5.jar # 金蝶 API 网关客户端 JAR 包
│   ├── compile.bat           # Java 编译脚本
│   ├── run.bat               # Java 运行脚本
│   ├── bin/                  # 编译输出目录
│   └── com/                  # 金蝶库源码
├── Python/                    # Python 版本实现
│   └── apigw_client.py       # 完整的 Python SDK 实现
├── CSharp/                    # C# 版本实现
│   ├── ApigwClient.cs        # C# SDK 实现
│   ├── ApigwClientLib.cs     # 辅助类库
│   ├── SignTest.cs           # 签名测试
│   ├── TestApigwClient.cs    # 测试程序
│   ├── ApigwClient.csproj           # C# 项目文件
│   ├── CSharpApiClient/      # C# 项目目录
│   └── obj/                  # 编译输出目录
└── README.md                 # 本文档
```

## 🔑 关键概念

### 认证机制

API 网关使用双重签名机制：

#### 1. **App Signature**（应用签名）
用于生成 `app_key` 和 `app_signature`，只需生成一次。

**算法**：
```
HMAC-SHA256(app_secret, client_id) 
  → 十六进制编码 
  → Base64 编码
```

**示例**：
```
Client ID:     328301
Client Secret: 1a97dac4f8c92a482424bf7732b115a1

生成过程：
1. HMAC-SHA256("328301", "1a97dac4f8c92a482424bf7732b115a1")
2. 转换为十六进制字符串
3. Base64 编码得到 app_signature
```

#### 2. **X-Api-Signature**（请求签名）
每次 API 调用都需要生成，用于验证请求的完整性和真实性。

**签名格式**：
```
Method + "\n" 
  + Path(URL encode, /→%2F) + "\n" 
  + QueryString(key不encode, value2x encode) + "\n" 
  + Headers(key小写) + "\n"
```

**签名头顺序**（必须）：
```
x-api-nonce
x-api-timestamp
```

**编码规则**：
- **Path**：1次 URL 编码，`/` 转换为 `%2F`
- **Query 参数**：key 不编码，value 进行2次 URL 编码
- **Headers**：全部转换为小写，格式为 `key:value`
- **最后一行**：Headers 后也需要 `\n`

**示例**：
```
POST
/api/v1/user/get
app_key=yJKZ3QLA&version=2
x-api-nonce:12345
x-api-timestamp:1704067200
```

### 核心类

#### ApigwClient（单例）
主要 API 客户端类，用于发送请求。

**主要方法**：
- `getInstance()` - 获取单例实例
- `setConfig(config)` - 设置配置
- `send(request)` - 发送 API 请求

#### ApiRequest
代表一个 API 请求。

**属性**：
- `path` - API 路径（如 `/api/v1/user/get`）
- `method` - HTTP 方法（GET, POST, PUT, DELETE）
- `headers` - 请求头字典
- `queryParams` - 查询参数字典
- `body` - 请求体（JSON）

#### ApiResult
代表 API 响应。

**属性**：
- `statusCode` - HTTP 状态码
- `headers` - 响应头字典
- `body` - 响应体（JSON 字符串）

#### ApigwConfig
API 网关配置。

**属性**：
- `clientId` - 客户端 ID
- `clientSecret` - 客户端密钥
- `hostUrl` - 网关地址（默认 `https://api.kingdee.com`）

---

## 📖 使用方法

### Java 版本

#### 编译
```bash
cd Java
compile.bat
```

或手动编译（需要 Java 17+）：
```bash
javac -cp apigwclient-0.1.5.jar TestApp.java
```

#### 运行
```bash
run.bat
```

或直接运行：
```bash
java -cp .;apigwclient-0.1.5.jar --add-exports java.base/sun.security.action=ALL-UNNAMED TestApp
```

#### 代码示例
```java
import com.kingdee.eacloud.apigw.ApigwClient;
import com.kingdee.eacloud.apigw.ApiRequest;
import com.kingdee.eacloud.apigw.ApiResult;
import com.kingdee.eacloud.apigw.HttpMethod;

public class TestApp {
    public static void main(String[] args) throws Exception {
        // 1. 获取单例实例
        ApigwClient client = ApigwClient.getInstance();
        
        // 2. 配置客户端
        client.setClientId("328301");
        client.setClientSecret("1a97dac4f8c92a482424bf7732b115a1");
        
        // 3. 创建请求
        ApiRequest request = new ApiRequest();
        request.setPath("/api/v1/user/get");
        request.setMethod(HttpMethod.POST);
        request.addQueryParam("app_key", generatedAppKey);
        
        // 4. 发送请求
        ApiResult result = client.send(request);
        
        // 5. 处理响应
        System.out.println("Status: " + result.getStatusCode());
        System.out.println("Body: " + result.getBody());
    }
}
```

### Python 版本

#### 安装依赖
```bash
pip install requests
```

#### 运行
```bash
python apigw_client.py
```

#### 代码示例
```python
from apigw_client import ApigwClient, ApiRequest, ApigwConfig

# 1. 创建配置
config = ApigwConfig(
    client_id="328301",
    client_secret="1a97dac4f8c92a482424bf7732b115a1"
)

# 2. 获取客户端实例
client = ApigwClient.get_instance()
client.set_config(config)

# 3. 创建请求
request = ApiRequest(
    path="/api/v1/user/get",
    method="POST"
)
request.add_query_param("app_key", generated_app_key)

# 4. 发送请求
result = client.send(request)

# 5. 处理响应
print(f"Status: {result.status_code}")
print(f"Body: {result.body}")
```

### C# 版本

#### 编译
```bash
cd CSharp
dotnet build
```

或使用 Visual Studio：
```bash
devenv Java.sln /build Release
```

#### 运行
```bash
dotnet run
```

#### 代码示例
```csharp
using System;
using CSharpApiClient;

class Program {
    static async Task Main(string[] args) {
        // 1. 创建配置
        var config = new ApigwConfig {
            ClientId = "328301",
            ClientSecret = "1a97dac4f8c92a482424bf7732b115a1"
        };

        // 2. 获取客户端实例
        var client = ApigwClient.GetInstance();
        client.SetConfig(config);

        // 3. 创建请求
        var request = new ApiRequest {
            Path = "/api/v1/user/get",
            Method = "POST"
        };
        request.AddQueryParam("app_key", generatedAppKey);

        // 4. 发送请求
        var result = await client.SendAsync(request);

        // 5. 处理响应
        Console.WriteLine($"Status: {result.StatusCode}");
        Console.WriteLine($"Body: {result.Body}");
    }
}
```

---

## 🔐 签名算法详解

### App Signature 生成

```
输入: client_id, client_secret
输出: app_key, app_signature

步骤:
1. SHA256HMAC = HMAC-SHA256(client_id, client_secret)
2. hex_string = 转换为十六进制 (32字节 → 64字符)
3. app_signature = Base64(hex_string)
4. app_key = Base64(client_id)
```

### X-Api-Signature 生成

需要签名的头：
```
x-api-nonce:      随机数 (如 12345)
x-api-timestamp:  当前时间戳 (秒)
```

签名内容格式：
```
Method + \n
Path(1xEncode) + \n
QueryString(keyNotEncode, value2xEncode) + \n
Headers(lowercase) + \n
```

**完整示例**：
```
请求方法: POST
路径: /api/v1/access/token
查询参数: app_key=yJKZ3QLA&version=2
请求体: {"grant_type":"client_credential"}

签名内容:
POST
/api/v1/access/token
app_key=yJKZ3QLA&version=2
x-api-nonce:12345
x-api-timestamp:1704067200

HMAC-SHA256 签名 → Base64 → X-Api-Signature
```

---

## ✅ 验证

### 签名测试

所有三个版本都包含了签名验证测试：

**测试用例**：
```
app_key = "abc"
appSecret = "abc123"
预期结果 = "ZDljMTI3NGIyNTE1MTRkYzlkNjc1MDNhYjUzMzgzNWMyY2M4YTdjMzdmNmM3YTVlNDkxMTkzNjdiOTFjNzUyZQ=="
```

**验证结果**：
```
Java:   ✅ 通过
C#:     ✅ 通过
Python: ✅ 通过
```

### API 调用验证

所有三个版本都成功通过了 API 测试：

```
HTTP 状态码: 200
响应状态:   {"errcode":0,"description":"成功"}
访问令牌:   已成功获取
```

---

## 📝 注意事项

### Java 版本
- **要求**：JDK 17 或更高版本
- **VM 参数**：`--add-exports java.base/sun.security.action=ALL-UNNAMED`
  - 原因：`sun.security.action.GetPropertyAction` 在 JDK 25 中被移除
- **依赖**：`apigwclient-0.1.5.jar`

### Python 版本
- **要求**：Python 3.7+
- **依赖**：`requests` 库
- **安装**：`pip install requests`

### C# 版本
- **要求**：.NET 9.0+
- **异步支持**：所有网络操作都是异步的
- **编译**：使用 `dotnet` CLI 或 Visual Studio

---

## 🔗 API 端点

### 主要端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/v1/access/token` | POST | 获取访问令牌 |
| `/api/v1/user/get` | POST | 获取用户信息 |

### 请求/响应示例

**请求**：
```json
POST /api/v1/access/token
Content-Type: application/json
X-Api-TimeStamp: 1704067200
X-Api-Nonce: 12345
X-Api-Signature: ...

{
  "grant_type": "client_credential"
}
```

**响应（成功）**：
```json
{
  "errcode": 0,
  "description": "成功",
  "data": {
    "access_token": "1762850879fbf059b28cc377fd3ebcc7",
    "app-token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
    "expires": 1767470851000,
    "uid": 173053163
  }
}
```

**响应（错误）**：
```json
{
  "errcode": -1,
  "description": "错误信息"
}
```

---

## 📞 故障排除

### 签名验证失败 (HTTP 519)
- 检查 `X-Api-Signature` 的生成逻辑
- 确保签名头顺序正确（`x-api-nonce` 在 `x-api-timestamp` 之前）
- 验证编码规则：路径1次编码，查询参数值2次编码

### 连接超时
- 检查网络连接
- 验证 `hostUrl` 配置是否正确
- 确保防火墙允许 HTTPS 连接

### 认证失败 (errcode != 0)
- 验证 `ClientId` 和 `ClientSecret` 是否正确
- 检查 `app_key` 和 `app_signature` 的生成是否正确
- 确保请求头中包含 `X-Api-Nonce` 和 `X-Api-TimeStamp`

---

## 📄 许可证

内部使用

## 👨‍💻 支持

如有问题，请参考各语言版本的测试文件：
- Java: `Java/TestApp.java`
- Python: `Python/apigw_client.py`
- C#: `CSharp/ApigwClient.cs`
