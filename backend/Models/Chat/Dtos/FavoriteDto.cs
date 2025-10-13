public class FavoriteDto {
  public long Id { get; set; }
  public long ConversationId { get; set; }
  public long? MessageId { get; set; }
  public string? Title { get; set; }
  public string? Note { get; set; }
  public decimal? SortOrder { get; set; }
  public DateTimeOffset PinnedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }
  public FavoriteMessageDto? Message { get; set; } // ← 新增
}