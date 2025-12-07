"""
金蝶API网关客户端 Python 实现
"""
import base64
import hashlib
import hmac
import random
import string
import time
from typing import Dict, Optional, List
from urllib.parse import quote
import requests


class HttpMethod:
    """HTTP 方法枚举"""
    GET = "GET"
    POST = "POST"
    POST_BODY = "POST"
    POST_FORM = "POST"
    DELETE = "DELETE"
    PUT = "PUT"
    PUT_BODY = "PUT"
    HEAD = "HEAD"


class ApigwConfig:
    """API 网关配置"""
    def __init__(self):
        self.client_id: str = ""
        self.client_secret: str = ""
        self.request_timeout: int = 30000
        self.connect_timeout: int = 10000
        self.response_timeout: int = 30000


class ApiRequest:
    """API 请求对象"""
    def __init__(self, method: str, host: str, path: str):
        self.method = method
        self.host = host
        self.path = path
        self.scheme = "https"
        self.headers: Dict[str, str] = {}
        self.querys: Dict[str, str] = {}
        self.body: Optional[bytes] = None
        self.timestamp: Optional[str] = None
        self.nonce: Optional[str] = None
        self.signature: Optional[str] = None
        self.sign_headers: List[str] = []

    def set_querys(self, querys: Dict[str, str]):
        """设置查询参数"""
        self.querys = querys

    def set_body_json(self, body: bytes):
        """设置 JSON 请求体"""
        self.body = body

    def add_header(self, key: str, value: str):
        """添加请求头"""
        self.headers[key] = value


class ApiResult:
    """API 响应结果"""
    def __init__(self, http_code: int, body: str, headers: Optional[Dict[str, str]] = None):
        self.http_code = http_code
        self.body = body
        self.headers = headers or {}


class ApigwClient:
    """API 网关客户端（单例模式）"""
    
    _instance = None
    _DEFAULT_SIGNHEADERS = ["X-Api-Nonce", "X-Api-TimeStamp"]
    _AUTH_VERSION = "2.0"
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(ApigwClient, cls).__new__(cls)
            cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        self._config: Optional[ApigwConfig] = None
        self._session = requests.Session()
        self._initialized = True
    
    @classmethod
    def get_instance(cls):
        """获取单例实例"""
        return cls()
    
    def init(self, config: ApigwConfig):
        """初始化配置"""
        self._config = config
        self._session.timeout = config.request_timeout / 1000.0
    
    def send(self, request: ApiRequest) -> ApiResult:
        """发送请求（同步）"""
        self._set_signature(request)
        return self._sync_request(request)
    
    def _set_signature(self, request: ApiRequest):
        """设置签名"""
        # 设置签名头
        if request.sign_headers:
            sign_headers = list(request.sign_headers)
            sign_headers.extend(self._DEFAULT_SIGNHEADERS)
            sign_headers.sort()
        else:
            sign_headers = list(self._DEFAULT_SIGNHEADERS)
        
        # 添加必要的头信息
        request.add_header("X-Api-ClientID", self._config.client_id)
        request.add_header("X-Api-Auth-Version", self._AUTH_VERSION)
        
        timestamp = self._get_timestamp(request)
        request.add_header("X-Api-TimeStamp", timestamp)
        
        nonce = self._get_nonce(10, request)
        request.add_header("X-Api-Nonce", nonce)
        
        request.add_header("X-Api-SignHeaders", ",".join(sign_headers))
        
        # 生成签名
        method = self._get_http_method_value(request.method)
        path_encode = self._get_path_encode(request.path)
        query_encode = self._get_query_encode_for_signature(request.querys)
        
        # 转换为小写的 header 名称
        lower_headers = {k.lower(): v for k, v in request.headers.items()}
        
        signature = self._get_signature(method, path_encode, query_encode, sign_headers, lower_headers)
        
        request.signature = signature
        request.add_header("X-Api-Signature", signature)
    
    def _get_http_method_value(self, method: str) -> str:
        """获取 HTTP 方法值"""
        if method in ["POST", "POST_BODY", "POST_FORM"]:
            return "POST"
        elif method in ["PUT", "PUT_BODY"]:
            return "PUT"
        else:
            return method.split("_")[0]
    
    def _get_timestamp(self, request: ApiRequest) -> str:
        """获取时间戳"""
        if request.timestamp:
            return request.timestamp
        return str(int(time.time() * 1000))
    
    def _get_nonce(self, length: int, request: ApiRequest) -> str:
        """获取随机数"""
        if request.nonce:
            return request.nonce
        
        chars = string.ascii_letters + string.digits
        return ''.join(random.choice(chars) for _ in range(length))
    
    def _get_path_encode(self, path: str) -> str:
        """Path 需要一次 URL 编码，/ 编码为 %2F"""
        result = []
        for c in path:
            if c == '/':
                result.append('%2F')
            elif c.isalnum() or c in '-_.~':
                result.append(c)
            else:
                result.append(quote(c).upper())
        return ''.join(result)
    
    def _get_query_encode(self, querys: Dict[str, str]) -> str:
        """不编码，仅用于原始值拼接"""
        if not querys:
            return ""
        
        sorted_items = sorted(querys.items())
        parts = [f"{k}={v}" for k, v in sorted_items]
        return "&".join(parts)
    
    def _get_query_encode_for_signature(self, querys: Dict[str, str]) -> str:
        """用于签名的 query 参数：key不编码，value两次编码"""
        if not querys:
            return ""
        
        sorted_items = sorted(querys.items())
        parts = []
        for key, value in sorted_items:
            # value 进行两次 URL 编码
            encoded_value = quote(value, safe='')
            encoded_value = quote(encoded_value, safe='')
            parts.append(f"{key}={encoded_value}")
        
        return "&".join(parts)
    
    def _get_signature(self, method: str, path: str, query: str, 
                      sign_headers: List[str], headers: Dict[str, str]) -> str:
        """生成签名"""
        # 构建签名原文
        lines = []
        lines.append(method)
        lines.append(path)
        
        # query 参数，如果为空也要有一行
        lines.append(query)
        
        # 添加签名头（必须是小写的 header 名称）
        sign_headers_lower = [h.lower() for h in sign_headers]
        for header_key in sign_headers_lower:
            if header_key in headers:
                lines.append(f"{header_key}:{headers[header_key]}")
        
        # 每行都以换行符结尾（包括最后一行）
        sign_content = "\n".join(lines) + "\n"
        
        # 使用 HMAC-SHA256 签名并转为16进制
        hmac_hex = self._sha256_hmac(sign_content, self._config.client_secret)
        
        # Base64 编码16进制字符串
        signature = base64.b64encode(hmac_hex.encode('utf-8')).decode('utf-8')
        
        return signature
    
    def _sha256_hmac(self, data: str, key: str) -> str:
        """HMAC-SHA256 签名并返回16进制字符串"""
        key_bytes = key.encode('utf-8')
        data_bytes = data.encode('utf-8')
        
        hmac_obj = hmac.new(key_bytes, data_bytes, hashlib.sha256)
        return hmac_obj.hexdigest()
    
    def _sync_request(self, request: ApiRequest) -> ApiResult:
        """发送同步请求"""
        # 构建完整URL
        url = f"{request.scheme}://{request.host}{request.path}"
        if request.querys:
            # URL 中的 query 参数需要编码
            sorted_items = sorted(request.querys.items())
            parts = [f"{quote(k, safe='')}={quote(v, safe='')}" for k, v in sorted_items]
            query_string = "&".join(parts)
            url += "?" + query_string
        
        # 设置 HTTP 方法
        method_map = {
            "GET": self._session.get,
            "POST": self._session.post,
            "DELETE": self._session.delete,
            "PUT": self._session.put,
            "HEAD": self._session.head
        }
        
        http_method = request.method.split("_")[0]
        request_func = method_map.get(http_method, self._session.get)
        
        # 准备请求参数
        kwargs = {
            "headers": request.headers,
            "timeout": self._config.request_timeout / 1000.0
        }
        
        if request.body:
            kwargs["data"] = request.body
            if "Content-Type" not in request.headers:
                kwargs["headers"]["Content-Type"] = "application/json;charset=utf-8"
        
        # 发送请求
        response = request_func(url, **kwargs)
        
        # 提取响应头
        response_headers = {k: v for k, v in response.headers.items()}
        
        return ApiResult(response.status_code, response.text, response_headers)


class SignatureUtil:
    """签名工具类"""
    
    @staticmethod
    def generate_signature(app_key: str, app_secret: str) -> str:
        """
        生成 app_signature 签名
        算法：SHA256HMAC(appKey, appSecret) -> 16进制字符串 -> Base64编码
        """
        hmac_hex = SignatureUtil.sha256_hmac(app_key, app_secret)
        base64_signature = base64.b64encode(hmac_hex.encode('utf-8')).decode('utf-8')
        return base64_signature
    
    @staticmethod
    def sha256_hmac(data: str, key: str) -> str:
        """HMAC-SHA256 签名并返回16进制字符串"""
        key_bytes = key.encode('utf-8')
        data_bytes = data.encode('utf-8')
        
        hmac_obj = hmac.new(key_bytes, data_bytes, hashlib.sha256)
        return hmac_obj.hexdigest()


def test_kingdee_auth_token(client_id:str, client_secret:str):
    """测试主函数 - 测试获取授权令牌"""
    print("开始调用API网关接口...")

    try:
        # 1. 创建配置
        config = ApigwConfig()
        config.client_id = client_id
        config.client_secret = client_secret
        print(f"已设置client_id: {client_id}")
        print("已设置client_secret")

        # 2. 初始化客户端
        apigw_client = ApigwClient.get_instance()
        apigw_client.init(config)
        print("已获取ApigwClient实例")
        print("已初始化API网关客户端")

        # 3. 创建请求
        request = ApiRequest(
            HttpMethod.GET,
            "api.kingdee.com",
            "/jdyconnector/app_management/kingdee_auth_token"
        )
        print("已创建ApiRequest对象")

        # 4. 设置查询参数
        print("准备设置app_key参数...")

        # 生成签名
        app_key = "yJKZ3QLA"
        app_secret = "a36777d74db08668cd00f4c8d3205a884db716fe"

        # 验证算法
        print("=== 验证签名算法 ===")
        test_signature = SignatureUtil.generate_signature("abc", "abc123")
        print(f"测试签名 (app_key=abc, appSecret=abc123): {test_signature}")
        print("预期结果: ZDljMTI3NGIyNTE1MTRkYzlkNjc1MDNhYjUzMzgzNWMyY2M4YTdjMzdmNmM3YTVlNDkxMTkzNjdiOTFjNzUyZQ==")
        print(f"验证结果: {'通过' if test_signature == 'ZDljMTI3NGIyNTE1MTRkYzlkNjc1MDNhYjUzMzgzNWMyY2M4YTdjMzdmNmM3YTVlNDkxMTkzNjdiOTFjNzUyZQ==' else '失败'}")

        # 生成实际签名
        app_signature = SignatureUtil.generate_signature(app_key, app_secret)
        print("=== 实际签名 ===")
        print(f"app_key: {app_key}")
        print(f"app_signature: {app_signature}")

        querys = {
            "app_key": app_key,
            "app_signature": app_signature
        }
        request.set_querys(querys)
        print(f"已设置app_key: {app_key}")
        print("已设置app_signature")
        print("已设置查询参数")

        # 5. 设置请求体
        request.set_body_json(b"{}")
        print("已设置请求体，准备发送请求...")

        # 6. 发送请求
        result = apigw_client.send(request)
        print("请求发送成功!")
        print(f"响应状态码: {result.http_code}")
        print(f"响应内容: {result.body}")

    except Exception as e:
        print("====== 异常详细信息 ======")
        print(f"异常类型: {type(e).__name__}")
        print(f"异常消息: {str(e)}")
        print("====== 堆栈跟踪 ======")
        import traceback
        traceback.print_exc()


def test_push_app_authorize(client_id:str, client_secret:str, outer_instance_id:str):
    """测试push_app_authorize接口 - 推送应用授权（API网关签名方式）"""
    print("\n\n" + "="*60)
    print("开始测试 push_app_authorize 接口...")
    print("="*60)

    try:
        # 1. 创建配置
        config = ApigwConfig()
        config.client_id = client_id
        config.client_secret = client_secret
        print(f"已设置client_id: {client_id}")
        print("已设置client_secret")

        # 2. 初始化客户端
        apigw_client = ApigwClient.get_instance()
        apigw_client.init(config)
        print("已获取ApigwClient实例")
        print("已初始化API网关客户端")

        # 3. 创建请求
        request = ApiRequest(
            HttpMethod.POST,
            "api.kingdee.com",
            "/jdyconnector/app_management/push_app_authorize"
        )
        print("已创建ApiRequest对象，路径: /jdyconnector/app_management/push_app_authorize")

        # 4. 设置查询参数（仅 outerInstanceId，不包含 app_key 和 app_signature）
        print("准备设置查询参数...")

        querys = {
            "outerInstanceId": outer_instance_id
        }
        request.set_querys(querys)
        print(f"已设置outerInstanceId: {outer_instance_id}")
        print("（注：app_key 和 app_signature 不通过 URL 参数传递）")

        # 5. 设置请求体
        request.set_body_json(b"{}")
        print("已设置请求体为空JSON")

        # 6. 发送请求（ApigwClient 会自动生成 API 网关签名并放在请求头中）
        print("\n准备发送请求，系统会自动生成签名...")
        print("请求头将包含:")
        print("  - X-Api-ClientID: 328301")
        print("  - X-Api-Auth-Version: 2.0")
        print("  - X-Api-TimeStamp: (当前时间戳)")
        print("  - X-Api-Nonce: (随机数)")
        print("  - X-Api-SignHeaders: X-Api-TimeStamp,X-Api-Nonce")
        print("  - X-Api-Signature: (HMAC-SHA256签名，基于上述头)")
        print("  - Content-Type: application/json;charset=utf-8")
        
        result = apigw_client.send(request)
        print("\n✓ 请求发送成功!")
        print(f"响应状态码: {result.http_code}")
        
        if result.http_code == 200:
            print(f"✓ 响应内容: {result.body[:200]}...")
        else:
            print(f"✗ 响应内容: {result.body}")

    except Exception as e:
        print("====== 异常详细信息 ======")
        print(f"异常类型: {type(e).__name__}")
        print(f"异常消息: {str(e)}")
        print("====== 堆栈跟踪 ======")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    # 运行测试函数
    clientid = "xxxxxx"
    clientidsecret = "xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
    outerinstanceid = "xxxxx"
    test_kingdee_auth_token(clientid, clientidsecret)
    test_push_app_authorize(clientid, clientidsecret, outerinstanceid)