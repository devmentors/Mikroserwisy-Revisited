using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Services.Tickets.Core.Messaging.Publishing;
using TicketFlow.Shared.Commands;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.SynchronousIntegration;

internal sealed class CreateTicketSynchronouslyHandler(
    ITicketsRepository repository,
    IMessagePublisher messagePublisher) : ICommandHandler<CreateTicketSynchronously>
{
    public async Task HandleAsync(CreateTicketSynchronously command, CancellationToken cancellationToken = default)
    {
        var (id, personToken, title, description, translatedDescription, category, languageCode, _) = command;

        var categoryValid = Enum.TryParse<TicketCategory>(category, out var categoryParsed);
        if (categoryValid is false)
        {
            categoryParsed = TicketCategory.Other;
        }

        var ticket = new Ticket(id, null, personToken, title, description, categoryParsed, languageCode);
        ticket.SetTranslation(translatedDescription);

        await repository.AddAsync(ticket, cancellationToken);

        var ticketCreatedMessage = new TicketCreated(
            Id: ticket.Id,
            InquiryId: id,
            PersonToken: ticket.PersonToken,
            Title: ticket.Title,
            Description: ticket.Description,
            Category: ticket.Category.ToString(),
            LanguageCode: ticket.LanguageCode,
            Version: ticket.Version);

        await messagePublisher.PublishAsync(ticketCreatedMessage, cancellationToken: cancellationToken);
    }
}