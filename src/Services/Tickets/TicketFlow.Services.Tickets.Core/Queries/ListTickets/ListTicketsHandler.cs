using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Tickets.Core.Data;
using TicketFlow.Services.Tickets.Core.Http;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.ListTickets;

public class ListTicketsHandler(TicketsDbContext dbContext, IPersonalInfoVaultClient vaultClient) : IQueryHandler<ListTicketsQuery, TicketsListDto>
{
    public async Task<TicketsListDto> HandleAsync(ListTicketsQuery query, CancellationToken cancellationToken = default)
    {
        var (agentId, status, page, limit) = query;

        var dbQuery = dbContext.Tickets.AsQueryable();

        if (query.AgentId is not null)
        {
            dbQuery = dbQuery.Where(x => x.AssignedTo.Equals(query.AgentId));
        }

        dbQuery = dbQuery.OrderByDescending(x => x.CreatedAt);

        var total = await dbQuery.CountAsync(cancellationToken);
        var tickets = await dbQuery
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Batch detokenization
        var tokens = tickets.Select(t => t.PersonToken).Distinct().ToList();
        var personalInfos = await vaultClient.GetBatchAsync(tokens, cancellationToken);
        var piiByToken = personalInfos.ToDictionary(p => p.PersonToken);

        var data = tickets.Select(x =>
        {
            var pii = piiByToken.GetValueOrDefault(x.PersonToken);
            return new TicketsListEntryDto(
                x.Id.ToString(),
                pii?.Name ?? "Unknown",
                pii?.Email ?? "Unknown",
                x.Title,
                x.Description,
                x.TranslatedDescription,
                x.Category,
                x.Status,
                x.CreatedAt,
                x.Severity,
                x.AssignedTo,
                x.LanguageCode,
                x.Type,
                x.DeadlineUtc,
                x.Resolution,
                x.QueuePosition,
                x.EscalatedToSupervisor,
                x.EscalationReason,
                x.EscalatedAt);
        }).ToList();

        return new(data, total);
    }
}