using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Data.Repositories;

public interface ITicketsRepository
{
    Task<Ticket?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task AddScheduledAction(TicketScheduledAction ticketScheduledAction, CancellationToken cancellationToken = default);
    Task<TicketScheduledAction?> GetScheduledAction(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate the queue position for a ticket in the WaitingForCapacity queue.
    /// Returns null if ticket is not in queue or position (1-indexed) if in queue.
    /// </summary>
    Task<int?> CalculateQueuePositionAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update queue positions for all tickets in WaitingForCapacity status.
    /// Called after assignments to recalculate positions.
    /// </summary>
    Task UpdateQueuePositionsAsync(CancellationToken cancellationToken = default);
}