// Models/Chat/MessageReaction.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class MessageReaction
{
  /// <summary>複合 PK（MessageId, UserId, Emoji）</summary>
  public long   MessageId { get; set; }
  public long   UserId    { get; set; }
  public string Emoji     { get; set; } = default!;

  public DateTimeOffset CreatedAt { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
