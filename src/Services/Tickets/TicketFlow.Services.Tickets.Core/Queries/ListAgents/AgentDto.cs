namespace TicketFlow.Services.Tickets.Core.Queries.ListAgents;

public record AgentDto(
    string Id,
    string UserId,
    string FullName,
    string Position,
    string AvatarUrl);