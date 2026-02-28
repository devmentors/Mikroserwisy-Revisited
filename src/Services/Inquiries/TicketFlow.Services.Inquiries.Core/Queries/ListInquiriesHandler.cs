using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Inquiries.Core.Data;
using TicketFlow.Services.Inquiries.Core.Http;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Inquiries.Core.Queries;

public class ListInquiriesHandler : IQueryHandler<ListInquiries, InquiriesListDto>
{
    private readonly InquiriesDbContext _dbContext;
    private readonly IPersonalInfoVaultClient _vaultClient;

    public ListInquiriesHandler(InquiriesDbContext dbContext, IPersonalInfoVaultClient vaultClient)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _vaultClient = vaultClient ?? throw new ArgumentNullException(nameof(vaultClient));
    }

    public async Task<InquiriesListDto> HandleAsync(ListInquiries query, CancellationToken cancellationToken = default)
    {
        var (page, limit, userId) = query;
        if (userId.HasValue is false)
        {
            throw new InvalidOperationException("Unauthorized!");
        }
        
        if (limit > 25)
        {
            limit = 25;
        }

        var dbQuery = _dbContext.Inquiries.AsQueryable();

        if (userId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.UserId == userId.Value);
        }

        var count = await dbQuery.CountAsync(cancellationToken);

        var data = await dbQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var tokens = data.Select(x => x.PersonToken).Distinct().ToList();
        var personalInfos = await _vaultClient.GetBatchAsync(tokens, cancellationToken);
        var piiByToken = personalInfos.ToDictionary(p => p.PersonToken);

        return new InquiriesListDto(
            Data: data.Select(x =>
            {
                var pii = piiByToken.GetValueOrDefault(x.PersonToken);
                return new InquiriesListEntryDto(
                    x.Id.ToString(),
                    pii?.Name ?? "Unknown",
                    x.Title,
                    pii?.Email ?? "Unknown",
                    x.Description,
                    x.Category,
                    x.Status,
                    x.CreatedAt.ToString("O"),
                    x.TicketId?.ToString());
            }).ToList(),
            TotalCount: count);
    }
}