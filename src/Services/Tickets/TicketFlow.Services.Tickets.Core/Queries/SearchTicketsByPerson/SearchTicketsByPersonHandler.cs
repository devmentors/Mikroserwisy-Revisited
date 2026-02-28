using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Tickets.Core.Data;
using TicketFlow.Services.Tickets.Core.Http;
using TicketFlow.Services.Tickets.Core.Queries.ListTickets;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Tickets.Core.Queries.SearchTicketsByPerson;

public class SearchTicketsByPersonHandler(TicketsDbContext dbContext, IPersonalInfoVaultClient vaultClient)
    : IQueryHandler<SearchTicketsByPerson, TicketsListDto>
{
    public async Task<TicketsListDto> HandleAsync(SearchTicketsByPerson query, CancellationToken cancellationToken = default)
    {
        if (query.PersonTokens is null || query.PersonTokens.Count == 0)
        {
            return new TicketsListDto(new List<TicketsListEntryDto>(), 0);
        }

        var tickets = await dbContext.Tickets
            .Where(x => query.PersonTokens.Contains(x.PersonToken))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

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
                x.Resolution);
        }).ToList();

        return new TicketsListDto(data, data.Count);
    }
}
