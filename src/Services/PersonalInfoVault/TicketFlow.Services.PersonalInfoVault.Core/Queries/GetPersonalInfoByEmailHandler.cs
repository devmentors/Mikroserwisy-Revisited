using TicketFlow.Services.PersonalInfoVault.Core.DTOs;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.PersonalInfoVault.Core.Queries;

internal sealed class GetPersonalInfoByEmailHandler(
    IPersonalInfoRepository repository) : IQueryHandler<GetPersonalInfoByEmail, PersonalInfoDto?>
{
    public async Task<PersonalInfoDto?> HandleAsync(
        GetPersonalInfoByEmail query,
        CancellationToken cancellationToken = default)
    {
        var personalInfo = await repository.GetByEmailAsync(query.Email, cancellationToken);
        if (personalInfo is null)
        {
            return null;
        }

        return new PersonalInfoDto(
            personalInfo.PersonToken,
            personalInfo.Name,
            personalInfo.Email,
            personalInfo.IsAnonymized);
    }
}
