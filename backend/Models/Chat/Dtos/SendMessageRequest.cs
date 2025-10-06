namespace SocialApp.Api.Models.Chat.Dtos;

public sealed record class SendMessageRequest
{
    public string Kind { get; set; } = "text"; // 先支援 text
    public string? Text { get; set; }
    public string? ClientMessageId { get; set; } // 前端去重鍵（可選）
    public long? ReplyToMessageId { get; set; }   // ← 加上這個
}
