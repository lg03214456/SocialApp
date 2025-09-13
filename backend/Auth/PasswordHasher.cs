using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace SocialApp.Api.Auth;

public class PasswordHasher(IConfiguration cfg)
{
    private readonly int _time = cfg.GetValue<int>("Argon2:TimeCost", 3);
    private readonly int _memKb = cfg.GetValue<int>("Argon2:MemoryMB", 128) * 1024;
    private readonly int _par = cfg.GetValue<int>("Argon2:Parallelism", 2);
    private readonly string _pepper = cfg.GetValue<string>("Argon2:Pepper") ?? "";

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Compute(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Compute(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private byte[] Compute(string password, byte[] salt)
    {
        var pwd = Encoding.UTF8.GetBytes(password + _pepper);
        var a = new Argon2id(pwd)
        {
            Salt = salt,
            Iterations = _time,
            MemorySize = _memKb,
            DegreeOfParallelism = _par
        };
        return a.GetBytes(32);
    }
}
