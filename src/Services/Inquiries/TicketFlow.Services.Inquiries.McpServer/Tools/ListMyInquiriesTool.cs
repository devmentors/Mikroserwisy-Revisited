using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using TicketFlow.Services.Inquiries.McpServer.Models;
using TicketFlow.Shared.Mcp;

namespace TicketFlow.Services.Inquiries.McpServer.Tools;

public class ListMyInquiriesTool
{
    private readonly InquiriesApiClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ListMyInquiriesTool> _logger;

    public ListMyInquiriesTool(InquiriesApiClient client, IHttpContextAccessor httpContextAccessor, ILogger<ListMyInquiriesTool> logger)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [McpServerTool(Name = "list_my_inquiries")]
    [Description("List your inquiries with status summary. Returns overview of your submissions and their current processing state.")]
    public async Task<string> ExecuteAsync(
        [Description("Page number (default 1)")]
        int page = 1,
        [Description("Results per page (default 10, max 50)")]
        int limit = 10,
        CancellationToken ct = default)
    {
        var userId = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
        _logger.LogInformation("ListMyInquiries called with X-User-Id: '{UserId}'", userId ?? "NULL");

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in request headers");
            return ErrorResponse.Create(
                "User ID not found in request context.",
                "Ensure you are logged in and try again."
            ).ToJson();
        }

        _logger.LogInformation("Fetching inquiries for user: {UserId}", userId);
        var inquiries = await _client.ListInquiriesAsync(userId, page, Math.Min(limit, 50), ct);
        _logger.LogInformation("Found {Count} inquiries for user {UserId}", inquiries.TotalCount, userId);

        // Group by status for overview
        var statusBreakdown = inquiries.Data
            .GroupBy(i => i.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Build semantic summary
        var summaryParts = new List<string>();

        if (inquiries.TotalCount == 0)
        {
            summaryParts.Add("You have no inquiries");
        }
        else
        {
            summaryParts.Add($"You have {inquiries.TotalCount} inquiry(ies)");

            var processing = statusBreakdown.GetValueOrDefault("Processing", 0);
            var completed = statusBreakdown.GetValueOrDefault("Completed", 0);
            var submitted = statusBreakdown.GetValueOrDefault("Submitted", 0);

            if (processing > 0) summaryParts.Add($"{processing} being processed");
            if (completed > 0) summaryParts.Add($"{completed} completed");
            if (submitted > 0) summaryParts.Add($"{submitted} awaiting processing");
        }

        // Build next steps
        var nextSteps = new List<string>();

        if (statusBreakdown.GetValueOrDefault("Completed", 0) > 0)
            nextSteps.Add("Use check_inquiry_status to get ticket details for completed inquiries");

        if (statusBreakdown.GetValueOrDefault("Processing", 0) > 0)
            nextSteps.Add("Your inquiries are being processed. Check back later for updates.");

        if (nextSteps.Count == 0 && inquiries.TotalCount == 0)
            nextSteps.Add("Submit a new inquiry to get started.");

        // Map inquiries with status descriptions
        var recentInquiries = inquiries.Data
            .Take(5)
            .Select(i => new InquiryInfo(
                i.Id,
                i.Title,
                i.Status.ToString(),
                GetStatusDescription(i.Status.ToString()),
                i.Category.ToString(),
                i.CreatedAt,
                i.TicketId
            ))
            .ToList();

        var response = new ListInquiriesResponse
        {
            Summary = string.Join(". ", summaryParts) + ".",
            TotalCount = inquiries.TotalCount,
            StatusBreakdown = statusBreakdown,
            RecentInquiries = recentInquiries.Count > 0 ? recentInquiries : null,
            NextSteps = nextSteps,
            Pagination = new PaginationInfo(
                page,
                limit,
                (int)Math.Ceiling(inquiries.TotalCount / (double)limit)
            )
        };

        return response.ToJson();
    }

    private static string GetStatusDescription(string status) => status switch
    {
        "Submitted" => "Awaiting processing",
        "Processing" => "Being processed by the team",
        "Completed" => "Completed - ticket created",
        "Rejected" => "Rejected",
        _ => "Unknown status"
    };
}
