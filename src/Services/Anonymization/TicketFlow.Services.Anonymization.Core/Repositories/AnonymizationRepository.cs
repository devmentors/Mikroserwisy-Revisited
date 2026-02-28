using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Anonymization.Core.Data;
using TicketFlow.Services.Anonymization.Core.Data.Models;

namespace TicketFlow.Services.Anonymization.Core.Repositories;

internal sealed class AnonymizationRepository(AnonymizationDbContext dbContext) : IAnonymizationRepository
{
    public async Task<AnonymizationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.AnonymizationRequests
            .Include(x => x.ServiceStatuses)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<AnonymizationRequest?> GetByPersonTokenAsync(string personToken, CancellationToken cancellationToken = default)
        => await dbContext.AnonymizationRequests
            .Include(x => x.ServiceStatuses)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.PersonToken == personToken, cancellationToken);

    public async Task<List<AnonymizationRequest>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.AnonymizationRequests
            .Include(x => x.ServiceStatuses)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AnonymizationRequest request, CancellationToken cancellationToken = default)
    {
        dbContext.AnonymizationRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AnonymizationRequest request, CancellationToken cancellationToken = default)
    {
        dbContext.AnonymizationRequests.Update(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
