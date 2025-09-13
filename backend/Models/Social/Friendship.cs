// Models/Friendship.cs
using SocialApp.Api.Models.Core;  // 引入 User 類別所在 namespace
using System.ComponentModel.DataAnnotations; // 為了 [Timestamp]
namespace SocialApp.Api.Models.Social;

public class Friendship
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long FriendUserId { get; set; }
    public string Status { get; set; } = "Accepted"; // Accepted / Blocked
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public User FriendUser { get; set; } = null!;
    // 已有 UpdatedAt，這裡只要補 RowVer
    [Timestamp]
    public byte[] RowVer { get; set; } = default!;
}