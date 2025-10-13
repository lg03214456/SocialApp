public class FavoriteMessageDto
{
    public long Id { get; set; }
    public string Kind { get; set; } = "text";   // "text" | "sticker" | "file"
    public string? Text { get; set; }
    public DateTimeOffset Time { get; set; }
    public bool Mine { get; set; }               // 此訊息是否我方送出
}