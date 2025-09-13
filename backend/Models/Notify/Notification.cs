// Models/Notification.cs
using SocialApp.Api.Models.Core;  // 引入 User 類別所在 namespace
using System.ComponentModel.DataAnnotations; // 為了 [Timestamp]

namespace SocialApp.Api.Models.Notify;

public class Notification
{
    public long NotificationId { get; set; }
    public long UserId { get; set; }
    public string Type { get; set; } = null!;   // FriendRequest / FriendAccepted ...
    public string Payload { get; set; } = "{}"; // 最小必要 JSON
    public bool IsRead { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }   // 非 nullable（DB 有 DEFAULT + SaveChanges 會自動寫）
    public User User { get; set; } = null!;

    [Timestamp]
    public byte[] RowVer { get; set; } = default!;
}