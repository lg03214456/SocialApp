namespace SocialApp.Api.Models.Chat.Dtos;

public sealed record class FriendInboxRowDto
{
    public long   FriendId  { get; init; }
    public string FriendUid { get; init; } = default!;
    public string FriendName{ get; init; } = default!;

    public long?  ConversationId { get; init; }
    public long?  ReadUpToMessageId { get; init; }
    public int    UnreadCount { get; init; }

    public LastMessageDto? LastMessage { get; init; }
}
