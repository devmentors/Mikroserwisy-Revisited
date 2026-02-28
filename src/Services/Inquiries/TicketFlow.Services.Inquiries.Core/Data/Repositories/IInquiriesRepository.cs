using TicketFlow.Services.Inquiries.Core.Data.Models;

namespace TicketFlow.Services.Inquiries.Core.Data.Repositories;

public interface IInquiriesRepository
{
    Task<IEnumerable<Inquiry>> GetByPersonTokenAsync(string personToken, CancellationToken cancellationToken = default);
    Task<Inquiry?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Inquiry inquiry, CancellationToken cancellationToken = default);
    Task UpdateAsync(Inquiry inquiry, CancellationToken cancellationToken = default);
}