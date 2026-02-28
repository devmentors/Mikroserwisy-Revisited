using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Aggregation.Core.Data;
using TicketFlow.Services.Aggregation.Core.Models;

namespace TicketFlow.Services.Aggregation.Core.Repositories;

public class TicketProjectionRepository(AggregationDbContext dbContext) : ITicketProjectionRepository
{
    public async Task<TicketProjection> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.TicketProjections.FindAsync([id], cancellationToken);
    }

    public async Task<List<TicketProjection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TicketProjections
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TicketProjection>> GetByPersonTokensAsync(List<string> personTokens, CancellationToken cancellationToken = default)
    {
        if (personTokens is null || personTokens.Count == 0)
        {
            return new List<TicketProjection>();
        }

        return await dbContext.TicketProjections
            .Where(x => personTokens.Contains(x.PersonToken))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TicketProjection projection, CancellationToken cancellationToken = default)
    {
        await dbContext.TicketProjections.AddAsync(projection, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TicketProjection projection, CancellationToken cancellationToken = default)
    {
        projection.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.TicketProjections.Update(projection);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TicketStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var projections = await dbContext.TicketProjections.ToListAsync(cancellationToken);

        var totalTickets = projections.Count;
        var resolvedTickets = projections.Count(p => p.Status == "Resolved" || p.Status == "Closed");
        var openTickets = totalTickets - resolvedTickets;
        var breachedSla = projections.Count(p => p.SlaBreached == true);
        var onTrackSla = projections.Count(p => p.SlaBreached == false && !p.SlaServiceCompleted);
        var unassignedTickets = projections.Count(p => p.AgentId == null);

        return new TicketStatistics(
            totalTickets,
            openTickets,
            resolvedTickets,
            breachedSla,
            onTrackSla,
            unassignedTickets);
    }
}
