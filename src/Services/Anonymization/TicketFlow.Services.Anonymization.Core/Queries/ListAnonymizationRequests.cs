using TicketFlow.Services.Anonymization.Core.DTOs;
using TicketFlow.Shared.Queries;

namespace TicketFlow.Services.Anonymization.Core.Queries;

public sealed record ListAnonymizationRequests : IQuery<List<AnonymizationRequestDto>>;
