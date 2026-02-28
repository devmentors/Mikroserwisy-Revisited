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

    public async Task<int?> CalculateQueuePositionAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.Tickets
            .Where(t => t.Id == ticketId)
            .Select(t => new { t.Id, t.Status, t.CreatedAt })
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        // If ticket is already waiting, calculate its current position
        if (ticket.Status == TicketStatus.WaitingForCapacity)
        {
            var position = await dbContext.Tickets
                .Where(t => t.Status == TicketStatus.WaitingForCapacity && t.CreatedAt < ticket.CreatedAt)
                .CountAsync(cancellationToken);
            return position + 1;
        }

        // For new tickets being added to queue, get next position (count of waiting + 1)
        var waitingCount = await dbContext.Tickets
            .Where(t => t.Status == TicketStatus.WaitingForCapacity)
            .CountAsync(cancellationToken);
        return waitingCount + 1;
    }

    public async Task UpdateQueuePositionsAsync(CancellationToken cancellationToken = default)
    {
        var waitingTickets = await dbContext.Tickets
            .Where(t => t.Status == TicketStatus.WaitingForCapacity)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < waitingTickets.Count; i++)
        {
            var ticket = waitingTickets[i];
            var methodInfo = ticket.GetType().GetProperty(nameof(Ticket.QueuePosition));
            methodInfo?.SetValue(ticket, i + 1);  // Use reflection to set private setter
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}