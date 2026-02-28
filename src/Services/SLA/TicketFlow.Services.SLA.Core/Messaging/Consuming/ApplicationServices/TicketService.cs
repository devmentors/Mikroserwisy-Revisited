using Microsoft.Extensions.Logging;
using TicketFlow.Services.SLA.Core.Data.Models;
using TicketFlow.Services.SLA.Core.Data.Repositories;
using TicketFlow.Services.SLA.Core.Http.Tickets;
using TicketFlow.Services.SLA.Core.Messaging.Publishing;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming.ApplicationServices;

public class TicketService
{
    private readonly ITicketsClient _ticketsClient;
    private readonly ISLARepository _slaRepository;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketsClient ticketsClient,
        ISLARepository slaRepository,
        IMessagePublisher publisher,
        ILogger<TicketService> logger)
    {
        _ticketsClient = ticketsClient;
        _slaRepository = slaRepository;
        _publisher = publisher;
        _logger = logger;
    }
    
    public async Task HandleTicketQualifiedAsync(Guid ticketId, int version, CancellationToken cancellationToken = default)
    {
        var ticketDetails = await _ticketsClient.GetTicketDetails(ticketId.ToString(), cancellationToken);
        if (ticketDetails is null)
        {
            throw new TicketFlowException($"Could not fetch data of ticket: {ticketId}");
        }
        
        var serviceType = ticketDetails.Type.ParseAsServiceType() ?? ServiceType.Unknown;
        if (serviceType == ServiceType.Unknown)
        {
            throw new TicketFlowException("Unknown service type");
        }
        
        // Do we know this ticket based on reminders?
        var existingReminders = await _slaRepository.GetRemindersFor(
            serviceType: serviceType,
            serviceSourceId: ticketId.ToString(),
            cancellationToken);

        if (existingReminders is null) // First time qualified
        {
            var domain = new Email(ticketDetails.Email).Domain;
            var sla = await _slaRepository.GetSLAByRequestorDomain(domain, cancellationToken) ??
                      Defaults.SLA; // If no signed SLA - use defaults

            var deadline = sla.CalculatedDeadlineFor(ticketDetails.CreatedAt, serviceType,
                ticketDetails.SeverityLevel!.Value);

            if (deadline is not null)
            {
                var deadlineReminders = new DeadlineReminders(
                    serviceType,
                    ticketId.ToString(),
                    ticketDetails.AssignedAgentUserId,
                    ticketDetails.CreatedAt,
                    deadline);

                deadlineReminders.ServiceLastKnownVersion = version;
                await _slaRepository.SaveReminders(deadlineReminders, cancellationToken);

                await _publisher.PublishAsync(
                    new DeadlinesCalculated(
                        deadlineReminders.ServiceType,
                        deadlineReminders.ServiceSourceId,
                        deadlineReminders.DeadlineDateUtc),
                    cancellationToken: cancellationToken);
            }
        }
        else // We unblocked the ticket or reopened it
        {
            existingReminders.UpdateFromServiceChange(TicketStatus.Qualified);
            existingReminders.ServiceLastKnownVersion = version;
            await _slaRepository.SaveReminders(existingReminders, cancellationToken);
        }
    }

    public async Task HandleAgentAssignedAsync(Guid ticketId, int version, CancellationToken cancellationToken = default)
    {
        var ticketDetails = await _ticketsClient.GetTicketDetails(ticketId.ToString(), cancellationToken);
        if (ticketDetails is null)
        {
            throw new TicketFlowException($"Could not fetch data of ticket: {ticketId}");
        }
        
        var serviceType = ticketDetails.Type.ParseAsServiceType() ?? ServiceType.Unknown;
        if (serviceType == ServiceType.Unknown)
        {
            throw new TicketFlowException("Unknown service type");
        }
        
        var existingReminders = await _slaRepository.GetRemindersFor(
            serviceType: serviceType,
            serviceSourceId: ticketId.ToString(),
            cancellationToken);

        if (existingReminders is null)
        {
            throw new TicketFlowException($"Could not find existing reminders for ticket: {ticketId}");
        }
        
        existingReminders.UserIdToRemind = ticketDetails.AssignedAgentUserId;
        existingReminders.ServiceLastKnownVersion = version;
        await _slaRepository.SaveReminders(existingReminders, cancellationToken);
    }
    
    public async Task HandleTicketResolvedAsync(Guid ticketId, int version, CancellationToken cancellationToken = default)
    {
        var existingReminders = await _slaRepository.GetRemindersFor(
            anyOfServiceTypes: [ServiceType.QuestionTicket, ServiceType.IncidentTicket],
            serviceSourceId: ticketId.ToString(),
            cancellationToken);

        if (existingReminders is null)
        {
            throw new TicketFlowException($"Could not fetch data of ticket: {ticketId}");
        }
        
        existingReminders.UpdateFromServiceChange(TicketStatus.Resolved);
        existingReminders.ServiceLastKnownVersion = version;
        await _slaRepository.SaveReminders(existingReminders, cancellationToken);
    }

    public async Task MarkLastVersionKnownAsync(Guid ticketId, int version,
        CancellationToken cancellationToken = default)
    {
        var existingReminders = await _slaRepository.GetRemindersFor(
            anyOfServiceTypes: [ServiceType.QuestionTicket, ServiceType.IncidentTicket],
            serviceSourceId: ticketId.ToString(),
            cancellationToken);
        
        existingReminders.ServiceLastKnownVersion = version;
        await _slaRepository.SaveReminders(existingReminders, cancellationToken);
    }
}