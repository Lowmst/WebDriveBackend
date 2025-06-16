using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebDriveBackend.Utils;

public static class Jwt
{
    private static readonly byte[] Key = "test"u8.ToArray();

    // 此处不要用静态的 HMACSHA256 实例，这是线程不安全的，在某些情况下会引发异常
    // private static readonly HMACSHA256 Hmac = new(Key);
    // 下面的方法换成原生提供的静态方法
    private static readonly int ExpireMinute = 30;

    public static string Generate(string id)
    {
        var header =
            Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(new
            { sub = id, exp = DateTimeOffset.UtcNow.AddMinutes(ExpireMinute).ToUnixTimeSeconds() }));
        var signature =
            Base64Url.EncodeToString(HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes($"{header}.{payload}")));
        return $"{header}.{payload}.{signature}";
    }

    // 返回 sub
    public static JwtVerifyResult Verify(string token)
    {
        try
        {
            var jwt = token.Split(".");
            if (jwt.Length != 3) return new JwtVerifyResult { Succeeded = false };
            var header =
                JsonSerializer.Deserialize<Header>(Base64Url.DecodeFromChars(jwt[0]), JsonSerializerOptions.Web);
            var payload =
                JsonSerializer.Deserialize<Payload>(Base64Url.DecodeFromChars(jwt[1]), JsonSerializerOptions.Web);
            var signature = jwt[2];

            // 头部验证
            if (header!.Alg != "HS256" || header.Typ != "JWT")
                return new JwtVerifyResult { Succeeded = false, Errors = { "invalid token" } };

            // 签名验证
            if (signature !=
                Base64Url.EncodeToString(HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes($"{jwt[0]}.{jwt[1]}"))))
                return new JwtVerifyResult { Succeeded = false, Errors = { "invalid token" } };

            // 时间验证
            if (DateTimeOffset.UtcNow > DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(payload!.Exp)))
                return new JwtVerifyResult { Succeeded = false, Errors = { "expired token" } };

            // // Id验证
            // var user = db.UsersIdentity.FirstOrDefault(user => user.Id == payload.Sub);
            // if (user == null) return new ServiceResult() { Succeeded = false, Errors = { "invalid token" } };

            return new JwtVerifyResult { Succeeded = true, Id = payload.Sub };
        }
        catch (Exception)
        {
            return new JwtVerifyResult { Succeeded = false, Errors = { "invalid token" } };
        }
    }

    private class Header
    {
        public required string Alg { get; set; }
        public required string Typ { get; set; }
    }

    private class Payload
    {
        public required string Sub { get; set; }
        public required long Exp { get; set; }
    }

    private class JwtResult
    {
        public List<string> Errors { get; set; } = new();
    }
}

public class JwtVerifyResult
{
    public bool Succeeded { get; set; }
    public string? Id { get; set; }
    public List<string> Errors { get; set; } = new();
}