// Requests/AddFavoriteReq.cs
namespace SocialApp.Api.Models.Chat.Dtos;
public record AddFavoriteReq(
    long ConversationId,
    long? MessageId,
    string? Title,
    string? Note,
    decimal? SortOrder
);