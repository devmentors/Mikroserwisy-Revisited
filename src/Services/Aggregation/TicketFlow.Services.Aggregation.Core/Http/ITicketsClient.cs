namespace TicketFlow.Services.Aggregation.Core.Http;

public interface ITicketsClient
{
    Task<TicketDto> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<AgentDto> GetAgentAsync(Guid agentId, CancellationToken cancellationToken = default);
}

public record TicketDto(
    Guid Id,
    string? Name,
    string Email,
    string? Title,
    string? Description,
    string? Category,
    string Status,
    string? LanguageCode,
    string? SeverityLevel,
    Guid? AssignedAgentUserId);

public record AgentDto(
    string Id,
    string UserId,
    string FullName,
    string Position,
    string AvatarUrl);
