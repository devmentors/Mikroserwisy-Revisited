using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Inquiries.Core.Data.Models;

namespace TicketFlow.Services.Inquiries.Core.Data.Repositories;

internal sealed class InquiriesRepository(InquiriesDbContext dbContext) : IInquiriesRepository
{
    public async Task<IEnumerable<Inquiry>> GetByPersonTokenAsync(string personToken, CancellationToken cancellationToken = default)
        => await dbContext.Inquiries.Where(x => x.PersonToken == personToken).ToListAsync(cancellationToken);

    public async Task<Inquiry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Inquiries.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Inquiry inquiry, CancellationToken cancellationToken = default)
    {
        dbContext.Inquiries.Add(inquiry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Inquiry inquiry, CancellationToken cancellationToken = default)
    {
        dbContext.Update(inquiry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}