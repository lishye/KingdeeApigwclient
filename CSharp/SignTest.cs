using System;
using System.Security.Cryptography;
using System.Text;

// 简单测试签名
class SignTest
{
    static void Main()
    {
        string data = "GET\n/jdyconnector/app_management/kingdee_auth_token?app_key=yJKZ3QLA&app_signature=test\nX-Api-TimeStamp:1234567890\nX-Api-Nonce:test123";
        string key = "1a97dac4f8c92a482424bf7732b115a1";
        
        Console.WriteLine("待签名内容:");
        Console.WriteLine(data);
        Console.WriteLine($"\n待签名内容(转义): {data.Replace("\n", "\\n")}");
        Console.WriteLine($"\n密钥: {key}");
        
        string signature = SHA256HMAC(data, key);
        Console.WriteLine($"\n生成的签名: {signature}");
    }
    
    static string SHA256HMAC(string data, string key)
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
