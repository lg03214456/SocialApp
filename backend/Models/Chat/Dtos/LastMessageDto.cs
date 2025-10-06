// SocialApp.Api/Chat/Dtos/LastMessageDto.cs

namespace SocialApp.Api.Models.Chat.Dtos;

public sealed record class LastMessageDto
{
    public long Id { get; init; }         // MessageId
    public string Kind { get; init; } = default!; // "text" | "sticker" | "file"
    public string? Text { get; init; }         // 只有 text 有值
    public DateTimeOffset Time { get; set; }     = default!;
    public bool Mine { get; init; }         // 是否我發的
}