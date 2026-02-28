using System.Text.Json;
using Microsoft.Extensions.Logging;
using TicketFlow.Shared.Caching;

namespace TicketFlow.Services.Communication.Core.Http.Agents;

internal sealed class AgentClient(
    HttpClient httpClient,
    ICacheService cacheService,
    ILogger<AgentClient> logger) : IAgentClient
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);

    public async Task<AgentContactDto?> GetAgentAsync(string userId, CancellationToken ct = default)
    {
        var cacheKey = $"agent:{userId}";

        var cached = await cacheService.GetAsync<AgentContactDto>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogInformation(
                "Agent contact cache hit for UserId {UserId}. DisplayName: {DisplayName}",
                cached.UserId, cached.DisplayName);
            return cached;
        }

        logger.LogInformation(
            "Agent contact cache miss for UserId {UserId}. Fetching from Tickets service...",
            userId);

        try
        {
            var response = await httpClient.GetAsync($"/users/{userId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to get agent {UserId} from Tickets service. Status: {StatusCode}",
                    userId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var fullName = root.GetProperty("fullName").GetString() ?? "Unknown";

            var contactDto = new AgentContactDto(
                UserId: userId,
                DisplayName: fullName);

            await cacheService.SetAsync(cacheKey, contactDto, CacheExpiry, ct);

            logger.LogInformation(
                "Agent contact {UserId} fetched and cached. DisplayName: {DisplayName}",
                contactDto.UserId, contactDto.DisplayName);

            return contactDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching agent {UserId} from Tickets service", userId);
            return null;
        }
    }
}
