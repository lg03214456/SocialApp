// Models/Chat/Attachment.cs
using System.ComponentModel.DataAnnotations;

namespace SocialApp.Api.Models.Chat;

public class Attachment
{
  public long   AttachmentId     { get; set; }
  public string BlobKey          { get; set; } = default!; // 實體儲存 key（GUID）
  public string OriginalFileName { get; set; } = default!;
  public string MimeType         { get; set; } = default!;
  public long   SizeBytes        { get; set; }

  public string? ChecksumSha256 { get; set; }  // 可選：同檔重用
  public int?    Width          { get; set; }  // 圖片/影片
  public int?    Height         { get; set; }
  public int?    DurationMs     { get; set; }  // 音/影片

  public long           UploaderUserId { get; set; }
  public DateTimeOffset CreatedAt      { get; set; }

  [Timestamp] public byte[] RowVer { get; set; } = default!;
}
