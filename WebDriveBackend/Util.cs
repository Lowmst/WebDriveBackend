using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace WebDriveBackend;

public static class Util
{
    public static string GenerateId()
    {
        return Guid.NewGuid().ToString();
    }

    public static string GenerateSalt()
    {
        var rng = RandomNumberGenerator.Create();
        var bytes = new byte[64];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string HashPassword(string password, string salt)
    {
        return Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(password + salt)));
    }

    public static string DbSource(string source)
    {
        return "Data Source=" + source;
    }

    public static string GenerateRefreshToken()
    {
        var rng = RandomNumberGenerator.Create();
        var bytes = new byte[64];
        rng.GetBytes(bytes);
        return Base64Url.EncodeToString(bytes);
    }
}