using System.ComponentModel;
using ModelContextProtocol.Server;
using TicketFlow.Services.Tickets.McpServer.Models;
using TicketFlow.Shared.Mcp;

namespace TicketFlow.Services.Tickets.McpServer.Tools;

public class SearchResolvedTicketsTool
{
    private readonly TicketsApiClient _client;

    public SearchResolvedTicketsTool(TicketsApiClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "search_resolved_tickets")]
    [Description(
        "Search through resolved tickets to find previous solutions. " +
        "Returns tickets matching the query with their resolutions. " +
        "Useful for knowledge mining and finding answers to recurring issues.")]
    public async Task<string> ExecuteAsync(
        [Description("Search query - keywords to match against ticket title and description")]
        string query,
        [Description("Maximum results to return (default 5, max 20)")]
        int maxResults = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResolvedTicketsResponse
            {
                Summary = "Error: Search query is required.",
                NextSteps = ["Provide a search query to find resolved tickets."]
            }.ToJson();
        }
        
        var allResolved = await _client.ListTicketsAsync(
            agentId: null,
            status: "Resolved",
            page: 1,
            limit: 50,
            ct: ct);

        // Keyword search
        var keywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var matchingTickets = allResolved.Data
            .Select(t => new
            {
                Ticket = t,
                Score = CalculateMatchScore(t.Title, t.Description, t.Resolution, keywords)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(Math.Min(maxResults, 20))
            .Select(x => new ResolvedTicketMatch(
                x.Ticket.Id,
                x.Ticket.Title,
                TruncateText(x.Ticket.Description, 150),
                x.Ticket.Resolution ?? "Resolution not documented",
                x.Ticket.Category.ToString(),
                x.Score
            ))
            .ToList();

        // Semantic summary
        string summary;
        var nextSteps = new List<string>();

        if (matchingTickets.Count == 0)
        {
            summary = $"No resolved tickets found matching '{query}'. " +
                     $"Total resolved tickets in system: {allResolved.TotalCount}.";
            nextSteps.Add("Try different keywords or broader search terms.");
            nextSteps.Add("Check FAQ database for common answers.");
        }
        else
        {
            summary = $"Found {matchingTickets.Count} resolved ticket(s) matching '{query}'. " +
                     $"These contain potential solutions for similar issues.";

            var topMatch = matchingTickets[0];
            nextSteps.Add($"Best match: '{topMatch.Title}' - review resolution for applicable solution.");

            if (matchingTickets.Count > 1)
            {
                nextSteps.Add($"Found {matchingTickets.Count - 1} additional related ticket(s) with resolutions.");
            }
        }

        var response = new SearchResolvedTicketsResponse
        {
            Summary = summary,
            Matches = matchingTickets.Count > 0 ? matchingTickets : null,
            TotalResolvedInSystem = allResolved.TotalCount,
            NextSteps = nextSteps
        };

        return response.ToJson();
    }

    private static int CalculateMatchScore(string title, string description, string? resolution, string[] keywords)
    {
        var score = 0;
        var titleLower = title.ToLowerInvariant();
        var descLower = description.ToLowerInvariant();
        var resolutionLower = resolution?.ToLowerInvariant() ?? "";

        foreach (var keyword in keywords)
        {
            if (titleLower.Contains(keyword))
                score += 10;
            
            if (descLower.Contains(keyword))
                score += 5;
            
            if (resolutionLower.Contains(keyword))
                score += 8;
        }

        return score;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }
}

public sealed record SearchResolvedTicketsResponse : SemanticResponse
{
    public IReadOnlyList<ResolvedTicketMatch>? Matches { get; init; }
    public int TotalResolvedInSystem { get; init; }
}

public sealed record ResolvedTicketMatch(
    string TicketId,
    string Title,
    string DescriptionPreview,
    string Resolution,
    string Category,
    int RelevanceScore
);
