using System;
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class UserBookmark
{
    /// <summary>書籤主鍵</summary>
    public long Id { get; set; }

    /// <summary>誰的書籤</summary>
    [Required]
    public long UserId { get; set; }

    /// <summary>哪個聊天室</summary>
    [Required]
    public long ConversationId { get; set; }

    /// <summary>
    /// （可選）錨定的訊息 Id。允許同一聊天室多個書籤指向不同訊息。
    /// 若為 null，代表「對整個聊天室的書籤」而非特定訊息。
    /// </summary>
    public long? MessageId { get; set; }

    /// <summary>（可選）顯示用的標題/別名（如「待辦」、「重點段落」）</summary>
    [MaxLength(100)]
    public string? Title { get; set; }

    /// <summary>（可選）備註</summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// 手動排序；建議用 decimal 方便插隊（10, 10.5, 11）。
    /// 若為 null，前端可用 PinnedAt/UpdatedAt 排序。
    /// </summary>
    public decimal? SortOrder { get; set; }

    /// <summary>建立時間（加入書籤的時間）</summary>
    public DateTimeOffset PinnedAt { get; set; }

    /// <summary>更新時間</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>並發控制（SQL Server rowversion）</summary>

    [Timestamp] public byte[] RowVer { get; set; } = default!;
}
