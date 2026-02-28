using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class NotifySupervisorTool
{
    private readonly TicketsApiClient _ticketsClient;
    private readonly CommunicationApiClient _communicationClient;
    private readonly TicketsMcpServerOptions _options;

    public NotifySupervisorTool(
        TicketsApiClient ticketsClient,
        CommunicationApiClient communicationClient,
        IOptions<TicketsMcpServerOptions> options)
    {
        _ticketsClient = ticketsClient;
        _communicationClient = communicationClient;
        _options = options.Value;
    }

    [McpServerTool(Name = "notify_supervisor")]
    [Description("Send urgent notification to supervisor for critical tickets that cannot wait in queue. Use when ticket is high priority and no agents are available.")]
    public async Task<string> ExecuteAsync(
        [Description("Ticket ID (GUID)")] string ticketId,
        [Description("Optional reason for escalation")] string? reason = null,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(ticketId, out var guid))
        {
            return JsonSerializer.Serialize(new { error = "Invalid ticket ID format. Expected GUID." });
        }

        var ticket = await _ticketsClient.GetTicketAsync(guid, ct);
        if (ticket == null)
        {
            return JsonSerializer.Serialize(new { error = $"Ticket with ID {ticketId} not found." });
        }

        var escalationReason = reason ?? "No agent capacity - escalated by AI agent";
        var messageContent = $"""
            Ticket #{ticketId} - '{ticket.Title}' wymaga natychmiastowej uwagi.

            **Powód eskalacji:** {escalationReason}
            **Priorytet:** {ticket.SeverityLevel?.ToString() ?? "Unknown"}
            **Kategoria:** {ticket.Category?.ToString() ?? "Unknown"}
            **Klient:** {ticket.Email}
            **Status:** {ticket.Status}

            Klient oczekuje na rozwiązanie. Proszę o ręczne przypisanie tego ticketu.
            """;

        var notificationSent = await _communicationClient.SendMessageAsync(
            _options.SupervisorUserId,
            _options.SupervisorEmail,
            $"[PILNA ESKALACJA] Ticket #{ticketId}",
            messageContent,
            ct);

        if (!notificationSent)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Failed to send notification to supervisor",
                fallbackContact = _options.SupervisorEmail
            });
        }

        await _ticketsClient.AddClientNoteAsync(guid,
            $"[ESCALATION] Eskalowano do supervisora {_options.SupervisorEmail}. Powód: {escalationReason}", ct);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Supervisor {_options.SupervisorEmail} notified successfully",
            supervisorName = "Boguslaw Zlotowa",
            supervisorEmail = _options.SupervisorEmail,
            ticketId = ticketId
        });
    }
}

public class TicketsMcpServerOptions
{
    public Guid SupervisorUserId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string SupervisorEmail { get; set; } = "boguslaw.zlotowa@firma.pl";
    public string CommunicationApiBaseUrl { get; set; } = "http://localhost:5600";
}
