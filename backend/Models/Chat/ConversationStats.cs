// Models/Chat/ConversationStats.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class ConversationStats
{
  /// <summary>PK = FK 到 Conversation</summary>
  public long ConversationId { get; set; }

  public long?           LastMessageId      { get; set; }
  public string?         LastMessagePreview { get; set; }
  public DateTimeOffset? LastMessageAt      { get; set; }

  public int  MessageCount { get; set; }
  public int  MemberCount  { get; set; }

  /// <summary>每個會話自增的訊息序號（採用時用來穩定排序/錨點）</summary>
  public long NextMessageSeq { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
