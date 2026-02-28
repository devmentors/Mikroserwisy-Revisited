using System.Net.Http.Json;
using TicketFlow.Services.Tickets.Api.DTO;
using TicketFlow.Services.Tickets.Core.Queries.GetTicketDetails;
using TicketFlow.Services.Tickets.Core.Queries.ListAgents;
using TicketFlow.Services.Tickets.Core.Queries.ListTickets;

namespace TicketFlow.Services.Tickets.McpServer;

public class TicketsApiClient
{
    private readonly HttpClient _httpClient;

    public TicketsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public async Task<TicketsListDto> ListTicketsAsync(
        Guid? agentId = null,
        string? status = null,
        int page = 1,
        int limit = 10,
        CancellationToken ct = default)
    {
        var query = $"?page={page}&limit={limit}";
        if (agentId.HasValue) query += $"&agentId={agentId}";
        if (!string.IsNullOrEmpty(status)) query += $"&status={status}";

        var response = await _httpClient.GetAsync($"/tickets/{query}", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TicketsListDto>(JsonOptions, ct)
               ?? new TicketsListDto([], 0);
    }

    public async Task<TicketDetailsDto?> GetTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/tickets/{ticketId}/", ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<TicketDetailsDto>(JsonOptions, ct);
    }

    public async Task<AgentDto[]> ListAgentsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("/agents", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AgentDto[]>(ct) ?? [];
    }

    public async Task<AgentDto?> GetAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/agents/{agentId}", ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<AgentDto>(ct);
    }

    public async Task<AssignAgentResult> AssignAgentAsync(Guid ticketId, Guid agentId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync($"/tickets/{ticketId}/assign/{agentId}", null, ct);
        if (response.IsSuccessStatusCode)
            return new AssignAgentResult(true, null);

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        return new AssignAgentResult(false, errorBody);
    }

    public async Task<bool> ResolveTicketAsync(Guid ticketId, string resolution, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync(
            $"/tickets/{ticketId}/resolve/{Uri.EscapeDataString(resolution)}", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> GetClientNotesAsync(Guid ticketId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/tickets/{ticketId}/client-notes", ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<bool> AddClientNoteAsync(Guid ticketId, string note, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/tickets/{ticketId}/client-notes",
            new ClientNoteDto(note),
            ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> QualifyTicketAsync(
        Guid ticketId, string ticketType, string severityLevel, CancellationToken ct = default)
    {
        var body = new { TicketType = ticketType, SeverityLevel = severityLevel };
        var response = await _httpClient.PostAsJsonAsync($"/tickets/{ticketId}/qualify", body, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> BlockTicketAsync(Guid ticketId, string reason, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync(
            $"/tickets/{ticketId}/block/{Uri.EscapeDataString(reason)}", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnblockTicketAsync(Guid ticketId, string reason, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync(
            $"/tickets/{ticketId}/unblock/{Uri.EscapeDataString(reason)}", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<SetWaitingResult> SetTicketWaitingAsync(
        Guid ticketId, string reason, CancellationToken ct = default)
    {
        var body = new { Reason = reason };
        var response = await _httpClient.PostAsJsonAsync($"/tickets/{ticketId}/waiting", body, ct);

        if (!response.IsSuccessStatusCode)
        {
            return new SetWaitingResult(false, null, $"Failed: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<SetWaitingResult>(ct);
        return result ?? new SetWaitingResult(false, null, "Unknown error");
    }
}

public record SetWaitingResult(bool Success, int? QueuePosition, string Message);

public record AssignAgentResult(bool Success, string? ErrorBody);

public record AssignmentResult(bool Success, Guid? AssignedToAgentId, string? AssignedToAgentName, string Reasoning);

public record QualificationSuggestion(string Category, string Priority, string Reasoning);
