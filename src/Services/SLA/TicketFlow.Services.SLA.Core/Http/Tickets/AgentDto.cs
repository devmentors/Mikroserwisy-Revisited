namespace TicketFlow.Services.SLA.Core.Http.Tickets;

public record AgentDto(
    string Id,
    string UserId,
    string FullName,
    string Position,
    string AvatarUrl);