using TicketFlow.Services.Anonymization.Core.Data.Models;
using TicketFlow.Services.Anonymization.Core.DTOs;
using TicketFlow.Services.Anonymization.Core.Repositories;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Anonymization.Core.Queries;

internal sealed class GetAnonymizationRequestHandler(
    IAnonymizationRepository repository) : IQueryHandler<GetAnonymizationRequest, AnonymizationRequestDto?>
{
    public async Task<AnonymizationRequestDto?> HandleAsync(
        GetAnonymizationRequest query,
        CancellationToken cancellationToken = default)
    {
        var request = await repository.GetByIdAsync(query.Id, cancellationToken);
        return request is null ? null : MapToDto(request);
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
