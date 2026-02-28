using TicketFlow.Services.Communication.Core.Data.Models;
using TicketFlow.Services.Communication.Core.Http.Tickets;
using TicketFlow.Services.Communication.Core.Messages;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.Communication.Core.Messaging.Consuming;

public class TicketResolvedHandler(ITicketsClient ticketsClient, IMessageService messageService)
    : IMessageHandler<TicketResolved>
{
    public async Task HandleAsync(TicketResolved message, CancellationToken cancellationToken = default)
    {
        var ticketDetails = await ticketsClient.GetTicketDetails(message.TicketId.ToString(), cancellationToken);
        if (ticketDetails is null)
        {
            throw new TicketFlowException($"Could not fetch data of ticket: {message.TicketId}");
        }

        var responseMessage = new Message
        {
            RecipentEmail = ticketDetails.Email,
            Title = "Zgłoszenie zamknięte!",
            Content = $@"Zakończono procesowanie twojego zgłoszenia
                        ze statusem: [{ticketDetails.Status}],
                        rozwiązaniem: {ticketDetails.Resolution}",
            SenderUserId = ticketDetails.AssignedAgentUserId,
            Timestamp = DateTimeOffset.UtcNow
        };

        await messageService.SaveMessageAsync(responseMessage, ticketDetails.LanguageCode, cancellationToken);
    }
}