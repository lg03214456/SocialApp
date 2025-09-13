// // Models/User.cs
// namespace SocialApp.Api.Models;

// public class User
// {
//     public long Id { get; set; }
//     public string Username { get; set; } = default!;
//     public string PasswordHash { get; set; } = default!;
//     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
//     public string Email { get; set; } = default!;
//     public string UserId { get; set; } = default!;
//     public List<RefreshToken> RefreshTokens { get; set; } = [];
// }


// Models/User.cs
using SocialApp.Api.Models.Auth;
using System.ComponentModel.DataAnnotations; 
namespace SocialApp.Api.Models.Core;

public class User
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!; // ShowLogin Name
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Email { get; set; } = default!; // 你原本設 IsRequired，我沿用
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
    // 已有 UpdatedAt，這裡只要補 RowVer
    [Timestamp]
    public byte[] RowVer { get; set; } = default!;
}