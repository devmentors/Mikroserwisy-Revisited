using TicketFlow.Services.Anonymization.Core.Data.Models;
using TicketFlow.Services.Anonymization.Core.DTOs;
using TicketFlow.Services.Anonymization.Core.Repositories;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Anonymization.Core.Queries;

internal sealed class ListAnonymizationRequestsHandler(
    IAnonymizationRepository repository) : IQueryHandler<ListAnonymizationRequests, List<AnonymizationRequestDto>>
{
    public async Task<List<AnonymizationRequestDto>> HandleAsync(
        ListAnonymizationRequests query,
        CancellationToken cancellationToken = default)
    {
        var requests = await repository.GetAllAsync(cancellationToken);
        return requests.Select(MapToDto).ToList();
    }

    private static AnonymizationRequestDto MapToDto(AnonymizationRequest request) => new(
        request.Id,
        request.PersonToken,
        request.RequestedByEmail,
        request.Status.ToString(),
        request.CreatedAt,
        request.CompletedAt,
        request.ServiceStatuses.Select(s => new ServiceStatusDto(
            s.ServiceName,
            s.IsFinished,
            s.CompletedAt,
            s.ErrorMessage)).ToList());
}
