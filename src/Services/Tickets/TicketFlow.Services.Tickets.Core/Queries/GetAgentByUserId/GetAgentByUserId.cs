using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.GetAgentByUserId;

public record GetAgentByUserId(string UserId) : IQuery<AgentDetailsDto>;

public record AgentDetailsDto(
    string? Id,
    string? UserId,
    string? FullName,
    string? Position,
    string? AvatarUrl);
