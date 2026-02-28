using TicketFlow.Services.PersonalInfoVault.Core.Data.Models;

namespace TicketFlow.Services.PersonalInfoVault.Core.Repositories;

public interface IPersonalInfoRepository
{
    Task<PersonalInfo?> GetByTokenAsync(string personToken, CancellationToken cancellationToken = default);
    Task<PersonalInfo?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<PersonalInfo>> GetByTokensAsync(List<string> tokens, CancellationToken cancellationToken = default);
    Task<List<PersonalInfo>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
    Task AddAsync(PersonalInfo personalInfo, CancellationToken cancellationToken = default);
    Task UpdateAsync(PersonalInfo personalInfo, CancellationToken cancellationToken = default);
}
