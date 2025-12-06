using System;
using System.Collections.Generic;
using System.Text;
using Kingdee.ApiGateway;

class TestApigwClient
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        Console.WriteLine("开始调用API网关接口...");
        
        try
        {
            // 1. 创建配置
            var config = new ApigwConfig
            {
                ClientID = "328301",
                ClientSecret = "1a97dac4f8c92a482424bf7732b115a1"
            };
            Console.WriteLine("已设置client_id: 328301");
            Console.WriteLine("已设置client_secret");
            
            // 2. 初始化客户端
            var apigwClient = ApigwClient.GetInstance();
            apigwClient.Init(config);
            Console.WriteLine("已获取ApigwClient实例");
            Console.WriteLine("已初始化API网关客户端");
            
            // 3. 创建请求
            var request = new ApiRequest(
                Kingdee.ApiGateway.HttpMethod.GET,
                "api.kingdee.com",
                "/jdyconnector/app_management/kingdee_auth_token"
            );
            Console.WriteLine("已创建ApiRequest对象");
            
            // 4. 设置查询参数
            Console.WriteLine("准备设置app_key参数...");
            
            // 生成签名
            string appKey = "yJKZ3QLA";
            string appSecret = "2fb248033d37a62bc7c8525e7757d57e106c6e4c";
            
            // 验证算法
            Console.WriteLine("=== 验证签名算法 ===");
            string testSignature = SignatureUtil.GenerateSignature("abc", "abc123");
            Console.WriteLine($"测试签名 (app_key=abc, appSecret=abc123): {testSignature}");
            Console.WriteLine("预期结果: ZDljMTI3NGIyNTE1MTRkYzlkNjc1MDNhYjUzMzgzNWMyY2M4YTdjMzdmNmM3YTVlNDkxMTkzNjdiOTFjNzUyZQ==");
            Console.WriteLine($"验证结果: {(testSignature == "ZDljMTI3NGIyNTE1MTRkYzlkNjc1MDNhYjUzMzgzNWMyY2M4YTdjMzdmNmM3YTVlNDkxMTkzNjdiOTFjNzUyZQ==" ? "✓ 通过" : "✗ 失败")}");
            
            // 生成实际签名
            string appSignature = SignatureUtil.GenerateSignature(appKey, appSecret);
            Console.WriteLine("=== 实际签名 ===");
            Console.WriteLine($"app_key: {appKey}");
            Console.WriteLine($"app_signature: {appSignature}");
            
            var querys = new Dictionary<string, string>
            {
                { "app_key", appKey },
                { "app_signature", appSignature }
            };
            request.SetQuerys(querys);
            Console.WriteLine($"已设置app_key: {appKey}");
            Console.WriteLine("已设置app_signature");
            Console.WriteLine("已设置查询参数");
            
            // 5. 设置请求体
            request.SetBodyJson(Encoding.UTF8.GetBytes("{}"));
            Console.WriteLine("已设置请求体，准备发送请求...");
            
            // 6. 发送请求
            ApiResult result = await apigwClient.SendAsync(request);
            Console.WriteLine("请求发送成功!");
            Console.WriteLine($"响应状态码: {result.HttpCode}");
            Console.WriteLine($"响应内容: {result.Body}");
        }
        catch (Exception e)
        {
            Console.WriteLine("====== 异常详细信息 ======");
            Console.WriteLine($"异常类型: {e.GetType().Name}");
            Console.WriteLine($"异常消息: {e.Message}");
            Console.WriteLine("====== 堆栈跟踪 ======");
            Console.WriteLine(e.StackTrace);
        }
    }
}
