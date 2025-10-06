namespace SocialApp.Api.Models.Chat.Dtos;

// 不可被繼承的 record（推薦）
public sealed record class ReadUpToRequest
{
    public long messageId { get; set; }
}

