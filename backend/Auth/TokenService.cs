using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using SocialApp.Api.Data;
using SocialApp.Api.Models.Core;
using SocialApp.Api.Models.Auth;


namespace SocialApp.Api.Auth;

public class TokenService(IConfiguration cfg, AppDbContext db)
{
    private readonly JwtOptions _jwt = cfg.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions
    {
        Issuer = "Dev", Audience = "DevClient", Secret = "CHANGE_ME"
    };
    private readonly int _accessMins = cfg.GetValue("Auth:AccessMinutes", 15);
    private readonly int _refreshDays = cfg.GetValue("Auth:RefreshDays", 7);
    private readonly string _cookieName = cfg.GetValue("Auth:RefreshCookieName", "rt");
    private readonly string _sameSite = cfg.GetValue("Auth:SameSite", "Strict");

    public string IssueAccessToken(User u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.Username)
        };
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_accessMins),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task IssueRefreshAsync(User u, HttpResponse response, CancellationToken ct = default)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = u.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshDays)
        });
        await db.SaveChangesAsync(ct);

        var cookie = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // 本機 http
            SameSite = _sameSite.Equals("None", StringComparison.OrdinalIgnoreCase)
                ? SameSiteMode.None : SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(_refreshDays),
            Path = "/"
        };
        response.Cookies.Append(_cookieName, raw, cookie);
    }

    public async Task<User?> ValidateRefreshAsync(HttpRequest req, CancellationToken ct = default)
    {
        if (!req.Cookies.TryGetValue(_cookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var rec = await db.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow, ct);
        return rec?.User;
    }

    public async Task RevokeRefreshAsync(HttpRequest req, HttpResponse resp, CancellationToken ct = default)
    {
        if (req.Cookies.TryGetValue(_cookieName, out var raw))
        {
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
            var rec = await db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
            if (rec != null && rec.RevokedAt == null)
            {
                rec.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
        resp.Cookies.Delete(_cookieName, new CookieOptions { Path = "/" });
    }
}
