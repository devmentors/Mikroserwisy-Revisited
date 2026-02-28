using TicketFlow.Services.Anonymization.Core.Data.Models;

namespace TicketFlow.Services.Anonymization.Core.Repositories;

public interface IAnonymizationRepository
{
    Task<AnonymizationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AnonymizationRequest?> GetByPersonTokenAsync(string personToken, CancellationToken cancellationToken = default);
    Task<List<AnonymizationRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AnonymizationRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(AnonymizationRequest request, CancellationToken cancellationToken = default);
}
