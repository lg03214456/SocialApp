using SocialApp.Api.Models.Core;  // 引入 User 類別所在 namespace
using System.ComponentModel.DataAnnotations; // 為了 [Timestamp]
namespace SocialApp.Api.Models.Social;

public class FriendRequest
{
    public long RequestId { get; set; }
    public long FromUserId { get; set; }
    public long ToUserId { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "Pending"; // Pending/Accepted/Rejected/Expired
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }   // 非 nullable（DB 有 DEFAULT + SaveChanges 會自動寫）

    public DateTimeOffset? RespondedAt { get; set; }

    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
    [Timestamp]
    public byte[] RowVer { get; set; } = default!;
}
