namespace TicketFlow.BFF.Http.Aggregation;

public interface IAggregationClient
{
    Task<List<TicketProjectionDto>> GetProjectionsAsync(CancellationToken cancellationToken = default);
    Task<TicketStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<List<TicketProjectionDto>> GetProjectionsByTokensAsync(List<string> tokens, CancellationToken cancellationToken = default);
}

public record TicketProjectionDto(
    Guid Id,
    Guid InquiryId,
    string Name,
    string Email,
    string Title,
    string Description,
    string Category,
    string LanguageCode,
    string Status,
    string? SeverityLevel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? AgentId,
    string AgentName,
    string AgentAvatarUrl,
    DateTimeOffset? SlaDeadlineUtc,
    bool? SlaBreached,
    bool SlaServiceCompleted,
    int Version);

public record TicketStatisticsDto(
    int TotalTickets,
    int OpenTickets,
    int ResolvedTickets,
    int BreachedSla,
    int OnTrackSla,
    int UnassignedTickets);
