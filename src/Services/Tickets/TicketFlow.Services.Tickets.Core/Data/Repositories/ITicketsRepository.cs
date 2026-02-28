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
}