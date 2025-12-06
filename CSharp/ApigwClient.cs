using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Kingdee.ApiGateway
{
    /// <summary>
    /// HTTP 方法枚举
    /// </summary>
    public enum HttpMethod
    {
        GET,
        POST_FORM,
        POST_FORM_DATA,
        POST_BODY,
        POST_FILE,
        DELETE,
        PUT_FORM,
        PUT_BODY,
        HEAD
    }

    /// <summary>
    /// API 网关配置
    /// </summary>
    public class ApigwConfig
    {
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public int RequestTimeout { get; set; } = 30000;
        public int ConnectTimeout { get; set; } = 10000;
        public int ResponseTimeout { get; set; } = 30000;
    }

    /// <summary>
    /// API 请求对象
    /// </summary>
    public class ApiRequest
    {
        public HttpMethod Method { get; }
        public string Host { get; set; }
        public string Path { get; set; }
        public string Scheme { get; set; } = "https";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Querys { get; set; } = new Dictionary<string, string>();
        public byte[] Body { get; set; }
        public string TimeStamp { get; set; }
        public string Nonce { get; set; }
        public string Signature { get; set; }
        public List<string> SignHeaders { get; set; } = new List<string>();

        public ApiRequest(HttpMethod method, string host, string path)
        {
            Method = method;
            Host = host;
            Path = path;
        }

        public void SetQuerys(Dictionary<string, string> querys)
        {
            Querys = querys;
        }

        public void SetBodyJson(byte[] body)
        {
            Body = body;
        }

        public void AddHeader(string key, string value)
        {
            Headers[key] = value;
        }
    }

    /// <summary>
    /// API 响应结果
    /// </summary>
    public class ApiResult
    {
        public int HttpCode { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public ApiResult(int httpCode, string body, Dictionary<string, string> headers = null)
        {
            HttpCode = httpCode;
            Body = body;
            Headers = headers ?? new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// API 网关客户端
    /// </summary>
    public class ApigwClient
    {
        private static readonly ApigwClient _instance = new ApigwClient();
        private static readonly string[] DEFAULT_SIGNHEADERS = new[] { "X-Api-TimeStamp", "X-Api-Nonce" };
        private static readonly string AUTH_VERSION = "2.0";
        private static readonly Random _random = new Random();
        private static readonly HttpClient _httpClient = new HttpClient();

        private ApigwConfig _config;

        private ApigwClient() { }

        public static ApigwClient GetInstance()
        {
            return _instance;
        }

        public void Init(ApigwConfig config)
        {
            _config = config;
            _httpClient.Timeout = TimeSpan.FromMilliseconds(config.RequestTimeout);
        }

        public async Task<ApiResult> SendAsync(ApiRequest request)
        {
            SetSignature(request);
            return await SyncRequest(request);
        }

        public ApiResult Send(ApiRequest request)
        {
            return SendAsync(request).GetAwaiter().GetResult();
        }

        private void SetSignature(ApiRequest request)
        {
            // 设置签名头
            string[] signHeaders;
            if (request.SignHeaders != null && request.SignHeaders.Count > 0)
            {
                var headers = new List<string>(request.SignHeaders);
                headers.AddRange(DEFAULT_SIGNHEADERS);
                headers.Sort();
                signHeaders = headers.ToArray();
            }
            else
            {
                signHeaders = DEFAULT_SIGNHEADERS;
            }

            // 添加必要的头信息
            request.AddHeader("X-Api-ClientID", _config.ClientID);
            request.AddHeader("X-Api-Auth-Version", AUTH_VERSION);
            
            string timestamp = GetTimestamp(request);
            request.AddHeader("X-Api-TimeStamp", timestamp);
            
            string nonce = GetNonce(10, request);
            request.AddHeader("X-Api-Nonce", nonce);
            
            request.AddHeader("X-Api-SignHeaders", string.Join(",", signHeaders));

            // 生成签名
            string method = GetHttpMethodValue(request.Method);
            string pathEncode = GetPathEncode(request.Path);
            string queryEncode = GetQueryEncode(request.Querys);
            string signature = GetSignature(method, pathEncode, queryEncode, signHeaders, request.Headers);

            request.Signature = signature;
            request.AddHeader("X-Api-Signature", signature);
        }

        private string GetHttpMethodValue(HttpMethod method)
        {
            switch (method)
            {
                case HttpMethod.GET: return "GET";
                case HttpMethod.POST_BODY: return "POST";
                case HttpMethod.POST_FORM: return "POST";
                case HttpMethod.DELETE: return "DELETE";
                case HttpMethod.PUT_BODY: return "PUT";
                default: return method.ToString().Split('_')[0];
            }
        }

        private string GetTimestamp(ApiRequest request)
        {
            if (!string.IsNullOrEmpty(request.TimeStamp))
                return request.TimeStamp;
            
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        }

        private string GetNonce(int length, ApiRequest request)
        {
            if (!string.IsNullOrEmpty(request.Nonce))
                return request.Nonce;

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[_random.Next(chars.Length)]);
            }
            return sb.ToString();
        }

        private string GetPathEncode(string path)
        {
            return Uri.EscapeDataString(path).Replace("%2F", "/");
        }

        private string GetQueryEncode(Dictionary<string, string> querys)
        {
            if (querys == null || querys.Count == 0)
                return string.Empty;

            var sorted = querys.OrderBy(kv => kv.Key);
            var parts = sorted.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
            return string.Join("&", parts);
        }

        private string GetSignature(string method, string path, string query, string[] signHeaders, Dictionary<string, string> headers)
        {
            var sb = new StringBuilder();
            sb.Append(method).Append("\n");
            sb.Append(path);
            if (!string.IsNullOrEmpty(query))
            {
                sb.Append("?").Append(query);
            }
            sb.Append("\n");

            // 添加签名头
            for (int i = 0; i < signHeaders.Length; i++)
            {
                string headerKey = signHeaders[i];
                if (headers.ContainsKey(headerKey))
                {
                    sb.Append(headerKey).Append(":").Append(headers[headerKey]);
                    if (i < signHeaders.Length - 1)
                    {
                        sb.Append("\n");
                    }
                }
            }

            string signContent = sb.ToString();
            
            // 使用 HMAC-SHA256 签名
            return SHA256HMAC(signContent, _config.ClientSecret);
        }

        private string SHA256HMAC(string data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(dataBytes);
                
                // 转换为16进制字符串
                var sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private async Task<ApiResult> SyncRequest(ApiRequest request)
        {
            // 构建完整URL
            string url = $"{request.Scheme}://{request.Host}{request.Path}";
            if (request.Querys != null && request.Querys.Count > 0)
            {
                string query = GetQueryEncode(request.Querys);
                url += "?" + query;
            }

            // 创建请求
            var httpRequest = new HttpRequestMessage();
            httpRequest.RequestUri = new Uri(url);

            // 设置HTTP方法
            switch (request.Method)
            {
                case HttpMethod.GET:
                    httpRequest.Method = System.Net.Http.HttpMethod.Get;
                    break;
                case HttpMethod.POST_BODY:
                case HttpMethod.POST_FORM:
                    httpRequest.Method = System.Net.Http.HttpMethod.Post;
                    if (request.Body != null)
                    {
                        httpRequest.Content = new ByteArrayContent(request.Body);
                        httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    }
                    break;
                case HttpMethod.DELETE:
                    httpRequest.Method = System.Net.Http.HttpMethod.Delete;
                    break;
                case HttpMethod.PUT_BODY:
                    httpRequest.Method = System.Net.Http.HttpMethod.Put;
                    if (request.Body != null)
                    {
                        httpRequest.Content = new ByteArrayContent(request.Body);
                    }
                    break;
            }

            // 添加请求头
            foreach (var header in request.Headers)
            {
                if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    // 如果无法添加到请求头，尝试添加到内容头
                    httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // 发送请求
            var response = await _httpClient.SendAsync(httpRequest);
            
            // 读取响应
            string body = await response.Content.ReadAsStringAsync();
            int statusCode = (int)response.StatusCode;

            // 提取响应头
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            return new ApiResult(statusCode, body, responseHeaders);
        }
    }

    /// <summary>
    /// 签名工具类
    /// </summary>
    public class SignatureUtil
    {
        /// <summary>
        /// 生成 app_signature 签名
        /// 算法：SHA256HMAC(appKey, appSecret) -> 16进制字符串 -> Base64编码
        /// </summary>
        public static string GenerateSignature(string appKey, string appSecret)
        {
            string hmacHex = SHA256HMAC(appKey, appSecret);
            byte[] hmacBytes = Encoding.UTF8.GetBytes(hmacHex);
            string base64Signature = Convert.ToBase64String(hmacBytes);
            return base64Signature;
        }

        public static string SHA256HMAC(string data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(dataBytes);
                
                var sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
