using TicketFlow.CourseUtils;
using TicketFlow.Services.Tickets.Core.Data.Models;
using TicketFlow.Services.Tickets.Core.Data.Repositories;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Tickets.Core.Messaging.Consuming.InquirySubmitted;

public sealed class InquirySubmittedHandler(ITicketsRepository repository, IMessagePublisher messagePublisher)
    : IMessageHandler<InquirySubmitted>, IMessageHandler<Shared.Contracts.Inquiries.Events.InquirySubmitted>
{
    public Task HandleAsync(InquirySubmitted message, CancellationToken cancellationToken = default)
        => ProcessInquiry(message.Id, message.PersonToken, message.Title,
            message.Description, message.Category, message.LanguageCode, cancellationToken);

    public Task HandleAsync(Shared.Contracts.Inquiries.Events.InquirySubmitted message, CancellationToken cancellationToken = default)
        => ProcessInquiry(message.Id, message.PersonToken, message.Title,
            message.Description, message.Category, message.LanguageCode, cancellationToken);

    private async Task ProcessInquiry(
        Guid id, string personToken, string title,
        string description, string category, string languageCode,
        CancellationToken cancellationToken)
    {
        if (!FeatureFlags.UseListenToYourselfExample)
        {
            await HandleDefault(id, personToken, title, description, category, languageCode, cancellationToken);
        }
        else
        {
            await HandleWithListenToYourself(id, personToken, title, description, category, languageCode, cancellationToken);
        }
    }

    private async Task HandleDefault(Guid id, string personToken, string title, string description,
        string category, string languageCode, CancellationToken cancellationToken)
    {
        if (await repository.ExistsAsync(id, cancellationToken))
        {
            return;
        }

        var categoryValid = Enum.TryParse<TicketCategory>(category, out var categoryParsed);
        if (categoryValid is false)
        {
            categoryParsed = TicketCategory.Other;
        }

        var ticket = new Ticket(id, personToken, title, description, categoryParsed, languageCode);

        var scheduledAction = await repository.GetScheduledAction(id, cancellationToken);

        if (scheduledAction is not null)
        {
            ticket.SetTranslation(scheduledAction.TranslatedText);
        }
        else if (ticket.IsEnglish is false)
        {
            ticket.WaitForScheduledActions();
        }

        await repository.AddAsync(ticket, cancellationToken);

        var ticketCreatedMessage = new Publishing.TicketCreated(
            Id: ticket.Id,
            InquiryId: id,
            PersonToken: ticket.PersonToken,
            Title: ticket.Title,
            Description: ticket.Description,
            Category: ticket.Category.ToString(),
            LanguageCode: ticket.LanguageCode);

        await messagePublisher.PublishAsync(ticketCreatedMessage, cancellationToken: cancellationToken);
    }

    private async Task HandleWithListenToYourself(Guid id, string personToken, string title, string description,
        string category, string languageCode, CancellationToken cancellationToken)
    {
        if (await repository.ExistsAsync(id, cancellationToken))
        {
            return;
        }

        var categoryValid = Enum.TryParse<TicketCategory>(category, out var categoryParsed);
        if (categoryValid is false)
        {
            categoryParsed = TicketCategory.Other;
        }

        var ticket = new Ticket(Guid.NewGuid(), personToken, title, description, categoryParsed, languageCode);

        var scheduledAction = await repository.GetScheduledAction(id, cancellationToken);

        if (scheduledAction is not null)
        {
            ticket.SetTranslation(scheduledAction.TranslatedText);
        }
        else if (ticket.IsEnglish is false)
        {
            ticket.WaitForScheduledActions();
        }

        var ticketCreatedMessage = new Publishing.TicketCreated(
            Id: ticket.Id,
            InquiryId: id,
            PersonToken: ticket.PersonToken,
            Title: ticket.Title,
            Description: ticket.Description,
            Category: ticket.Category.ToString(),
            LanguageCode: ticket.LanguageCode);

        await messagePublisher.PublishAsync(ticketCreatedMessage, cancellationToken: cancellationToken);
    }
}
