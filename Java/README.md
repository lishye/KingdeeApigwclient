# Java 版本

金蝶 API 网关客户端的 Java 实现。

## 文件说明

- **TestApp.java** - 完整的测试应用程序，演示 API 调用全过程
- **TestApp.class** - 编译后的字节码
- **apigwclient-0.1.5.jar** - 金蝶 API 网关客户端库
- **compile.bat** - 自动编译脚本
- **run.bat** - 自动运行脚本
- **bin/** - 编译输出目录
- **com/** - 金蝶库源码

## 快速开始

### 编译
```bash
compile.bat
```

### 运行
```bash
run.bat
```

## 需求

- JDK 17 或更高版本
- VM 参数：`--add-exports java.base/sun.security.action=ALL-UNNAMED`

## 功能

- ✅ App Signature 生成（HMAC-SHA256 → Hex → Base64）
- ✅ X-Api-Signature 请求签名
- ✅ API 请求发送和响应处理
- ✅ 错误处理和异常捕获

## 签名验证

程序包含内置的签名验证测试：
```
测试: app_key=abc, appSecret=abc123
验证结果: ✅ 通过
```

## 示例代码

```java
ApigwClient client = ApigwClient.getInstance();
client.setClientId("328301");
client.setClientSecret("1a97dac4f8c92a482424bf7732b115a1");

ApiRequest request = new ApiRequest();
request.setPath("/api/v1/access/token");
request.setMethod(HttpMethod.POST);

ApiResult result = client.send(request);
System.out.println("Status: " + result.getStatusCode());
```

## 输出示例

```
响应状态码: 200
响应内容: {"errcode":0,"description":"成功","data":{"access_token":"..."}}
```
