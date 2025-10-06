// Models/Chat/MessageAttachment.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class MessageAttachment
{
  /// <summary>複合 PK（MessageId, AttachmentId）</summary>
  public long MessageId    { get; set; }
  public long AttachmentId { get; set; }

  /// <summary>顯示名稱：同一訊息內唯一，重名後端會自動加 (1)</summary>
  public string DisplayName { get; set; } = default!;
  public int    SortOrder   { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
