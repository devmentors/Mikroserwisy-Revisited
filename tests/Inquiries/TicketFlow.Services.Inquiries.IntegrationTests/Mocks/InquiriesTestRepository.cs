using TicketFlow.Services.Inquiries.Core.Data.Models;
using TicketFlow.Services.Inquiries.Core.Data.Repositories;

namespace TicketFlow.Services.Inquiries.IntegrationTests.Mocks;

internal sealed class InquiriesTestRepository : IInquiriesRepository
{
    public Task<IEnumerable<Inquiry>> GetByPersonTokenAsync(string personToken, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<Inquiry>());

    public Task<Inquiry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(default(Inquiry));

    public Task AddAsync(Inquiry inquiry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;


    public Task UpdateAsync(Inquiry inquiry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}