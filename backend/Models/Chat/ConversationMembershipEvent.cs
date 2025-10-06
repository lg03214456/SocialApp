// Models/Chat/ConversationMembershipEvent.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class ConversationMembershipEvent
{
  public long EventId        { get; set; }
  public long ConversationId { get; set; }
  public long ActorUserId    { get; set; }
  public long? TargetUserId  { get; set; }

  /// <summary>invite|join|leave|remove|reject|promote|demote|rename|…</summary>
  public string EventType { get; set; } = default!;
  public string? ReasonCode { get; set; }
  public string? ReasonText { get; set; }

  public DateTimeOffset CreatedAt { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
