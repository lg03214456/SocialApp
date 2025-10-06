// Dtos/Chat/MessageItemDto.cs

namespace SocialApp.Api.Models.Chat.Dtos;

public sealed record class MessageItemDto
{
    public long Id { get; init; }      // 對應 Message.MessageId
    public string Kind { get; init; } = "text";
    public string? Text { get; init; }
    public DateTimeOffset Time { get; init; }   // ← 用 DateTimeOffset
    public bool Mine { get; init; }      // 前端要用來渲染左右
}