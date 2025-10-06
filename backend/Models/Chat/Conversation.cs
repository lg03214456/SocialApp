// Models/Chat/Conversation.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class Conversation
{
  public long ConversationId { get; set; }

  /// <summary>dm | group</summary>
  public string Type { get; set; } = "dm";

  // Group 相關
  public string? Title { get; set; }
  public string? AvatarUrl { get; set; }
  public string? Description { get; set; }
  public long?  OwnerUserId { get; set; }

  /// <summary>private | public …（可擴充）</summary>
  public string Visibility { get; set; } = "private";

  /// <summary>DM 用：規範 A<B（在 Fluent 做 CHECK）</summary>
  public long? DmUserAId { get; set; }
  public long? DmUserBId { get; set; }

  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
