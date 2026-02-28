namespace TicketFlow.Services.Tickets.Core.Queries.ListAgents;

public record AgentDto(
    Guid Id,
    string UserId,
    string FullName,
    string Position,
    string AvatarUrl,
    int AssignedTicketCount,
    int MaxTickets,
    string[] Specializations);