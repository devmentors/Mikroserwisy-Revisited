namespace TicketFlow.Services.Anonymization.Core.Http;

public interface IPersonalInfoVaultClient
{
    Task<string?> GetPersonTokenByEmailAsync(string email, CancellationToken cancellationToken = default);
}
