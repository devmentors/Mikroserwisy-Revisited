namespace TicketFlow.Services.Inquiries.Core.Http;

public interface IPersonalInfoVaultClient
{
    Task<string> StorePersonalInfoAsync(string name, string email, CancellationToken cancellationToken = default);
    Task<PersonalInfoDto?> GetAsync(string personToken, CancellationToken cancellationToken = default);
    Task<List<PersonalInfoDto>> GetBatchAsync(List<string> tokens, CancellationToken cancellationToken = default);
}
