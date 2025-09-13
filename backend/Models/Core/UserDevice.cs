// Models/UserDevice.cs
using SocialApp.Api.Models.Core;
using System.ComponentModel.DataAnnotations; // 為了 [Timestamp]

namespace SocialApp.Api.Models.Core;

public class UserDevice
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string DeviceId { get; set; } = null!;   // GUID 字串即可
    public string DeviceType { get; set; } = null!; // iOS/Android/Web…
    public string? PushToken { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    // 已有 UpdatedAt，這裡只要補 RowVer
    [Timestamp]
    public byte[] RowVer { get; set; } = default!;
}