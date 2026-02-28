namespace TicketFlow.Services.Tickets.Core.Http;

public interface IPersonalInfoVaultClient
{
    Task<PersonalInfoDto?> GetAsync(string personToken, CancellationToken cancellationToken = default);
    Task<List<PersonalInfoDto>> GetBatchAsync(List<string> tokens, CancellationToken cancellationToken = default);
}
