using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Data.Repositories;

internal sealed class TicketsRepository(TicketsDbContext dbContext) : ITicketsRepository
{
    public async Task<Ticket?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Tickets.AnyAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        await dbContext.Tickets.AddAsync(ticket, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        dbContext.Tickets.Update(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddScheduledAction(TicketScheduledAction ticketScheduledAction, CancellationToken cancellationToken = default)
    {
        await dbContext.TicketScheduledActions.AddAsync(ticketScheduledAction, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TicketScheduledAction?> GetScheduledAction(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.TicketScheduledActions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}