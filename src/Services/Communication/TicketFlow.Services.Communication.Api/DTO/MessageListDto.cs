using TicketFlow.Services.Communication.Core.Data.Models;

namespace TicketFlow.Services.Communication.Api.DTO;

public record MessageListDto(List<Message> Data, long Total);

public record MessageWithSenderDto(
    Guid Id,
    string? RecipentEmail,
    Guid? RecipentUserId,
    Guid? SenderEmployeeId,
    string SenderDisplayName,
    string Title,
    string Preview,
    string Content,
    DateTimeOffset Timestamp,
    bool IsRead);

public record MessageWithSenderListDto(List<MessageWithSenderDto> Data, long Total);