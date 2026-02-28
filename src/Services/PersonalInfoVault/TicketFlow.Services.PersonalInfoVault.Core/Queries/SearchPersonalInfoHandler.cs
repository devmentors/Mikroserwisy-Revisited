using TicketFlow.Services.PersonalInfoVault.Core.DTOs;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.PersonalInfoVault.Core.Queries;

internal sealed class SearchPersonalInfoHandler(
    IPersonalInfoRepository repository) : IQueryHandler<SearchPersonalInfo, SearchPersonalInfoDto>
{
    public async Task<SearchPersonalInfoDto> HandleAsync(
        SearchPersonalInfo query,
        CancellationToken cancellationToken = default)
    {
        var results = await repository.SearchAsync(query.Query, query.Limit, cancellationToken);

        var dtos = results.Select(p => new PersonalInfoDto(
            p.PersonToken,
            p.Name,
            p.Email,
            p.IsAnonymized)).ToList();

        return new SearchPersonalInfoDto(dtos);
    }
}
