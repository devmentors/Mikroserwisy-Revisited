using TicketFlow.Shared.Commands;

namespace TicketFlow.Services.Tickets.Core.SynchronousIntegration;

public sealed record CreateTicketSynchronously(
    Guid Id,
    string PersonToken,
    string Title,
    string Description,
    string TranslatedDescription,
    string Category,
    string LanguageCode,
    DateTimeOffset CreatedAt) : ICommand;