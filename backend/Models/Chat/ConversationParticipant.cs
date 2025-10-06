// Models/Chat/ConversationParticipant.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class ConversationParticipant
{
  /// <summary>複合 PK（ConversationId, UserId）</summary>
  public long ConversationId { get; set; }
  public long UserId         { get; set; }

  /// <summary>pending | member | rejected | left | removed</summary>
  public string Status { get; set; } = "member";
  /// <summary>owner | admin | member</summary>
  public string Role   { get; set; } = "member";

  // 已讀游標（只往前更新）
  public DateTimeOffset? LastReadAt        { get; set; }
  public long?           ReadUpToMessageId { get; set; }

  // 個人設定
  public bool             IsPinned  { get; set; }
  public DateTimeOffset?  MuteUntil { get; set; }

  // 進出群資訊
  public long?          InvitedByUserId { get; set; }
  public DateTimeOffset JoinedAt        { get; set; }
  public DateTimeOffset? LeftAt         { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
