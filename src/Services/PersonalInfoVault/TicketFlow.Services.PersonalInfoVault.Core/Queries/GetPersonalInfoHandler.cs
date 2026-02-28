using TicketFlow.Services.PersonalInfoVault.Core.DTOs;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.PersonalInfoVault.Core.Queries;

internal sealed class GetPersonalInfoHandler(
    IPersonalInfoRepository repository) : IQueryHandler<GetPersonalInfo, PersonalInfoDto?>
{
    public async Task<PersonalInfoDto?> HandleAsync(
        GetPersonalInfo query,
        CancellationToken cancellationToken = default)
    {
        var personalInfo = await repository.GetByTokenAsync(query.PersonToken, cancellationToken);
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
