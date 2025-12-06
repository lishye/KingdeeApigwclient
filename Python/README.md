# Python 版本

金蝶 API 网关客户端的 Python 实现。

## 文件说明

- **apigw_client.py** - 完整的 Python SDK 实现（370+ 行）
  - `ApigwConfig` - 配置类
  - `ApiRequest` - 请求类
  - `ApiResult` - 响应类
  - `ApigwClient` - 客户端类（单例）
  - `SignatureUtil` - 签名工具类

## 快速开始

### 安装依赖
```bash
pip install requests
```

### 运行
```bash
python apigw_client.py
```

## 需求

- Python 3.7 或更高版本
- 依赖包：`requests`

## 功能

- ✅ App Signature 生成（HMAC-SHA256 → Hex → Base64）
- ✅ X-Api-Signature 请求签名
- ✅ API 请求发送和响应处理
- ✅ 错误处理和异常捕获
- ✅ 完整的日志输出

## 签名验证

程序包含内置的签名验证测试：
```
测试: app_key=abc, appSecret=abc123
验证结果: 通过
```

## 示例代码

```python
from apigw_client import ApigwClient, ApiRequest, ApigwConfig

config = ApigwConfig(
    client_id="328301",
    client_secret="1a97dac4f8c92a482424bf7732b115a1"
)

client = ApigwClient.get_instance()
client.set_config(config)

request = ApiRequest(
    path="/api/v1/access/token",
    method="POST"
)

result = client.send(request)
print(f"Status: {result.status_code}")
print(f"Body: {result.body}")
```

## 输出示例

```
响应状态码: 200
响应内容: {"errcode":0,"description":"成功","data":{"access_token":"..."}}
```

## 类详解

### ApigwClient（单例）
```python
client = ApigwClient.get_instance()
client.set_config(config)
result = client.send(request)
```

### ApiRequest
```python
request = ApiRequest(path="/api/v1/user/get", method="POST")
request.add_query_param("key", "value")
request.add_header("X-Custom-Header", "value")
request.set_body({"data": "value"})
```

### ApiResult
```python
print(result.status_code)  # HTTP 状态码
print(result.body)         # 响应体（JSON 字符串）
print(result.headers)      # 响应头
```

### SignatureUtil
```python
# App Signature
app_sig = SignatureUtil.generate_app_signature("client_id", "client_secret")

# X-Api-Signature
sig = SignatureUtil.get_signature(method, path, query, headers, body)
```
