using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TicketFlow.CourseUtils;
using TicketFlow.Services.SLA.Core.Data.Models;
using TicketFlow.Services.SLA.Core.Data.Repositories;
using TicketFlow.Services.SLA.Core.Http.Billing;
using TicketFlow.Services.SLA.Core.Http.Tickets;
using TicketFlow.Services.SLA.Core.Messaging.Publishing;
using TicketFlow.Shared.Exceptions;
using TicketFlow.Shared.Messaging;

namespace TicketFlow.Services.SLA.Core.Messaging.Consuming.ApplicationServices;

public class TicketService
{
    private readonly ITicketsClient _ticketsClient;
    private readonly IBillingClient _billingClient;
    private readonly DirectLegacySoapProxy _legacySoapProxy;
    private readonly ISLARepository _slaRepository;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketsClient ticketsClient,
        IBillingClient billingClient,
        DirectLegacySoapProxy legacySoapProxy,
        ISLARepository slaRepository,
        IMessagePublisher publisher,
        ILogger<TicketService> logger)
    {
        _ticketsClient = ticketsClient;
        _billingClient = billingClient;
        _legacySoapProxy = legacySoapProxy;
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

            // Check payment standing and determine effective tier (may be downgraded)
            var effectiveTier = await DetermineEffectiveTierAsync(
                sla.ClientTier, domain, cancellationToken);

            var deadline = sla.CalculatedDeadlineFor(
                ticketDetails.CreatedAt,
                serviceType,
                ticketDetails.SeverityLevel!.Value,
                effectiveTier);

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
    
    private async Task<SLATier> DetermineEffectiveTierAsync(
        SLATier contractedTier, string domain, CancellationToken ct)
    {
        if (FeatureFlags.UseDirectLegacyBilling)
        {
            return await DetermineEffectiveTierFromLegacyDirectlyAsync(contractedTier, domain, ct);
        }

        var paymentStanding = await _billingClient.GetPaymentStandingAsync(domain, ct);

        // Graceful degradation: if billing unavailable, use contracted tier
        if (paymentStanding is null)
        {
            _logger.LogDebug(
                "Billing service unavailable for {Domain}. Using contracted tier: {Tier}",
                domain, contractedTier);
            return contractedTier;
        }

        // Customer in good standing - no downgrade needed
        if (paymentStanding.InGoodStanding)
        {
            _logger.LogDebug(
                "Customer {Domain} in good standing. Using contracted tier: {Tier}",
                domain, contractedTier);
            return contractedTier;
        }

        // Calculate downgrade based on days overdue
        var downgradeSteps = paymentStanding.DaysOverdue switch
        {
            >= 60 => 2,  // 60+ days: drop 2 tiers (VIP -> Standard)
            >= 30 => 1,  // 30+ days: drop 1 tier (VIP -> Premium)
            _ => 0       // Less than 30 days: no downgrade
        };

        var effectiveTier = DowngradeTier(contractedTier, downgradeSteps);

        if (effectiveTier != contractedTier)
        {
            _logger.LogWarning(
                "Customer {Domain} SLA downgraded {ContractedTier}->{EffectiveTier} " +
                "({DaysOverdue} days overdue, ${OverdueAmount} outstanding)",
                domain,
                contractedTier,
                effectiveTier,
                paymentStanding.DaysOverdue,
                paymentStanding.OverdueAmount);
        }

        return effectiveTier;
    }

    private async Task<SLATier> DetermineEffectiveTierFromLegacyDirectlyAsync(
        SLATier contractedTier, string domain, CancellationToken ct)
    {
        try
        {
            var xml = await _legacySoapProxy.GetCustomerInvoicesXmlAsync(domain, ct);

            if (xml.Contains("<soap:Fault>"))
            {
                _logger.LogWarning("Legacy billing returned SOAP Fault for {Domain}", domain);
                return contractedTier;
            }

            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://legacy.billing.corp/2005";
            var invoiceNodes = doc.Descendants(ns + "INVOICE");

            var maxDaysOverdue = 0;

            foreach (var inv in invoiceNodes)
            {
                var statusCode = inv.Element(ns + "STS_CD")?.Value;
                if (statusCode != "O") continue;

                var dueDateStr = inv.Element(ns + "DT_DUE")?.Value;
                if (DateTime.TryParseExact(dueDateStr, "yyyyMMdd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueDate))
                {
                    var daysOverdue = (DateTime.UtcNow - dueDate).Days;
                    maxDaysOverdue = Math.Max(maxDaysOverdue, daysOverdue);
                }
            }

            var downgradeSteps = maxDaysOverdue switch
            {
                >= 60 => 2,
                >= 30 => 1,
                _ => 0
            };

            return DowngradeTier(contractedTier, downgradeSteps);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Legacy billing unavailable for {Domain}. Using contracted tier.", domain);
            return contractedTier;
        }
    }

    private static SLATier DowngradeTier(SLATier tier, int steps)
    {
        var newValue = Math.Max((int)SLATier.Basic, (int)tier - steps);
        return (SLATier)newValue;
    }
}
