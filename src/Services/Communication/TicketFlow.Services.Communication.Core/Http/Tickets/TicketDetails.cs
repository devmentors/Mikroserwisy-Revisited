namespace TicketFlow.Services.Communication.Core.Http.Tickets;

public record TicketDto(
    string Id,
    string Email,
    string Status,
    Guid? AssignedAgentUserId,
    string? Resolution);