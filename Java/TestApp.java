import com.smecloud.apigw.client.ApigwClient;
import com.smecloud.apigw.constant.HttpMethod;
import com.smecloud.apigw.model.ApigwConfig;
import com.smecloud.apigw.model.ApiRequest;
import com.smecloud.apigw.model.ApiResult;
import com.smecloud.apigw.util.SHAUtil;

import java.lang.System.Logger;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;

public class TestApp {
    public static void main(String[] args) {
        Logger logger = System.getLogger(TestApp.class.getName());
        try {
            logger.log(System.Logger.Level.INFO, "开始调用API网关接口...");
            ApigwConfig config = new ApigwConfig();
            // 设置client_id
            config.setClientID("328301");
            logger.log(System.Logger.Level.INFO, "已设置client_id: 328301");
            // 设置client_secret
            config.setClientSecret("1a97dac4f8c92a482424bf7732b115a1");
            logger.log(System.Logger.Level.INFO, "已设置client_secret");
            ApigwClient apigwClient = ApigwClient.getInstance();
            logger.log(System.Logger.Level.INFO, "已获取ApigwClient实例");
            // 初始化API网关客户端
            apigwClient.init(config);
            logger.log(System.Logger.Level.INFO, "已初始化API网关客户端");
            ApiRequest request = new ApiRequest(HttpMethod.GET, "api.kingdee.com",
                    "/jdyconnector/app_management/kingdee_auth_token");
            logger.log(System.Logger.Level.INFO, "已创建ApiRequest对象");
            Map<String, String> map = new HashMap<>();
            logger.log(System.Logger.Level.INFO, "准备设置app_key参数...");
            
            // 生成签名
            String appKey = "yJKZ3QLA";
            String appSecret = "2fb248033d37a62bc7c8525e7757d57e106c6e4c";
            
            // 验证算法示例
            logger.log(System.Logger.Level.INFO, "=== 验证签名算法 ===");
            String testSignature = generateSignature("abc", "abc123");
            logger.log(System.Logger.Level.INFO, "测试签名 (app_key=abc, appSecret=abc123): " + testSignature);
            logger.log(System.Logger.Level.INFO, "预期结果: ZDljMTI3NGIyNTE1MTRkYzlkNjc1MDNhYjUzMzgzNWMyY2M4YTdjMzdmNmM3YTVlNDkxMTkzNjdiOTFjNzUyZQ==");
            logger.log(System.Logger.Level.INFO, "验证结果: " + (testSignature.equals("ZDljMTI3NGIyNTE1MTRkYzlkNjc1MDNhYjUzMzgzNWMyY2M4YTdjMzdmNmM3YTVlNDkxMTkzNjdiOTFjNzUyZQ==") ? "✓ 通过" : "✗ 失败"));
            
            // 生成实际签名
            String appSignature = generateSignature(appKey, appSecret);
            logger.log(System.Logger.Level.INFO, "=== 实际签名 ===");
            logger.log(System.Logger.Level.INFO, "app_key: " + appKey);
            logger.log(System.Logger.Level.INFO, "app_signature: " + appSignature);

            map.put("app_key", appKey);
            logger.log(System.Logger.Level.INFO, "已设置app_key: " + appKey);
            map.put("app_signature", appSignature);
            logger.log(System.Logger.Level.INFO, "已设置app_signature");
            request.setQuerys(map);
            logger.log(System.Logger.Level.INFO, "已设置查询参数");
            request.setBodyJson("{}".getBytes());
            logger.log(System.Logger.Level.INFO, "已设置请求体，准备发送请求...");
            ApiResult result = ApigwClient.getInstance().send(request);
            logger.log(System.Logger.Level.INFO, "请求发送成功!");
            logger.log(System.Logger.Level.INFO, "响应状态码: " + result.getHttpCode());
            logger.log(System.Logger.Level.INFO, "响应内容: " + result.getBody());
            
        } catch (Exception e) {
            logger.log(System.Logger.Level.ERROR, "发生异常: " + e.getMessage(), e);
            System.err.println("====== 异常详细信息 ======");
            System.err.println("异常类型: " + e.getClass().getName());
            System.err.println("异常消息: " + e.getMessage());
            System.err.println("====== 堆栈跟踪 ======");
            e.printStackTrace();
        }
    }
    
    /**
     * 生成 app_signature 签名
     * 算法：SHA256HMAC(appKey, appSecret) -> 16进制字符串 -> Base64编码
     * 
     * @param appKey 应用密钥
     * @param appSecret 应用密钥
     * @return Base64 编码的签名
     */
    private static String generateSignature(String appKey, String appSecret) {
        // 1. 使用 SHA256HMAC 生成签名（结果已经是16进制字符串）
        String hmacHex = SHAUtil.SHA256HMAC(appKey, appSecret);
        
        // 2. 将16进制字符串进行 Base64 编码
        String base64Signature = Base64.getEncoder().encodeToString(hmacHex.getBytes());
        
        return base64Signature;
    }
}