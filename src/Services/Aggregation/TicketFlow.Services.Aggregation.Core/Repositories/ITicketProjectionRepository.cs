using TicketFlow.Services.Aggregation.Core.Models;

namespace TicketFlow.Services.Aggregation.Core.Repositories;

public interface ITicketProjectionRepository
{
    Task<TicketProjection> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TicketProjection>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<TicketProjection>> GetByPersonTokensAsync(List<string> personTokens, CancellationToken cancellationToken = default);
    Task AddAsync(TicketProjection projection, CancellationToken cancellationToken = default);
    Task UpdateAsync(TicketProjection projection, CancellationToken cancellationToken = default);
    Task<TicketStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public record TicketStatistics(
    int TotalTickets,
    int OpenTickets,
    int ResolvedTickets,
    int BreachedSla,
    int OnTrackSla,
    int UnassignedTickets);
