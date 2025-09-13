// Models/RefreshToken.cs
using SocialApp.Api.Models.Core;  // 引入 User 類別所在 namespace

namespace SocialApp.Api.Models.Auth;

public class RefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}