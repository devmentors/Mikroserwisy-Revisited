namespace TicketFlow.Services.Communication.Core.Http.Tickets;

public record TicketDto(
    string Id,
    Guid? UserId,
    string Email,
    string Status,
    Guid? AssignedAgentUserId,
    string? Resolution);