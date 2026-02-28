namespace TicketFlow.BFF.Http.PersonalInfoVault;

public interface IPersonalInfoVaultClient
{
    Task<SearchPersonalInfoDto> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
}

public record SearchPersonalInfoDto(List<PersonalInfoDto> Results);

public record PersonalInfoDto(string PersonToken, string Name, string Email, bool IsAnonymized);
