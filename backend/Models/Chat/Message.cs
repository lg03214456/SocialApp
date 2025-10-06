// Models/Chat/Message.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class Message
{
  public long MessageId      { get; set; }
  public long ConversationId { get; set; }
  public long SenderId       { get; set; }

  /// <summary>text | sticker | image | file | system</summary>
  public string Kind { get; set; } = "text";

  // 各類型對應欄位（CHECK 約束在 Fluent 寫）
  public string? Text              { get; set; }
  public long?   StickerId         { get; set; }
  public string? MetaJson          { get; set; } // 圖片尺寸/時長/codec/hash 等
  public long?   ReplyToMessageId  { get; set; }

  /// <summary>每個會話內的連號（若採用）</summary>
  public long MessageSeq { get; set; }

  /// <summary>前端去重鍵：同一次重試用同一個（UNIQUE with ConversationId+SenderId）</summary>
  public string? ClientMessageId { get; set; }

  public DateTimeOffset CreatedAt   { get; set; }
  public DateTimeOffset? EditedAt   { get; set; }
  public int            EditedCount { get; set; }
  public DateTimeOffset? DeletedAt  { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
