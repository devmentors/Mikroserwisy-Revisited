using TicketFlow.Services.PersonalInfoVault.Core.DTOs;
using TicketFlow.Services.PersonalInfoVault.Core.Repositories;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.PersonalInfoVault.Core.Queries;

internal sealed class GetPersonalInfoBatchHandler(
    IPersonalInfoRepository repository) : IQueryHandler<GetPersonalInfoBatch, BatchResponse>
{
    public async Task<BatchResponse> HandleAsync(
        GetPersonalInfoBatch query,
        CancellationToken cancellationToken = default)
    {
        var personalInfos = await repository.GetByTokensAsync(query.Tokens, cancellationToken);
        var items = personalInfos.Select(p => new PersonalInfoDto(
            p.PersonToken,
            p.Name,
            p.Email,
            p.IsAnonymized)).ToList();

        return new BatchResponse(items);
    }
}
