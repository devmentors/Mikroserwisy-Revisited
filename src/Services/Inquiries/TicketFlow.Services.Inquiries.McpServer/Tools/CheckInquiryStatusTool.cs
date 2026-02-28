using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using TicketFlow.Services.Inquiries.Core.Queries;
using TicketFlow.Services.Inquiries.McpServer.Models;
using TicketFlow.Shared.Mcp;

namespace TicketFlow.Services.Inquiries.McpServer.Tools;

public class CheckInquiryStatusTool
{
    private readonly InquiriesApiClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CheckInquiryStatusTool(InquiriesApiClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    [McpServerTool(Name = "check_inquiry_status")]
    [Description("Check status of a specific inquiry by ID or related ticket ID. Returns detailed status with explanation.")]
    public async Task<string> ExecuteAsync(
        [Description("Inquiry ID - optional if ticketId is provided")]
        string? inquiryId = null,
        [Description("Related ticket ID - optional if inquiryId is provided")]
        string? ticketId = null,
        CancellationToken ct = default)
    {
        var userId = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(userId))
        {
            return ErrorResponse.Create(
                "User ID not found in request context.",
                "Ensure you are logged in and try again."
            ).ToJson();
        }

        if (string.IsNullOrEmpty(inquiryId) && string.IsNullOrEmpty(ticketId))
        {
            return ErrorResponse.Create(
                "Either inquiryId or ticketId must be provided.",
                "Provide at least one identifier to look up the inquiry."
            ).ToJson();
        }

        InquiriesListEntryDto? inquiry = null;

        if (!string.IsNullOrEmpty(inquiryId))
        {
            inquiry = await _client.GetInquiryByIdAsync(inquiryId, userId, ct);
        }
        else if (!string.IsNullOrEmpty(ticketId))
        {
            inquiry = await _client.GetInquiryByTicketIdAsync(ticketId, userId, ct);
        }

        if (inquiry == null)
        {
            return ErrorResponse.Create(
                "Inquiry not found or you don't have access to it.",
                "Verify the inquiry/ticket ID and ensure you are the owner."
            ).ToJson();
        }

        var status = inquiry.Status.ToString();
        var statusDescription = GetStatusDescription(status);

        // Build contextual summary and next steps
        var (summary, nextSteps) = status switch
        {
            "Submitted" => (
                $"Your inquiry '{inquiry.Title}' has been submitted and is awaiting processing.",
                new List<string> { "Your inquiry is in queue. Check back later for updates." }
            ),
            "Processing" => (
                $"Your inquiry '{inquiry.Title}' is currently being processed by our team.",
                new List<string> { "Processing is underway. You will be notified when completed." }
            ),
            "Completed" => (
                $"Your inquiry '{inquiry.Title}' has been completed. A support ticket has been created.",
                new List<string>
                {
                    inquiry.TicketId != null
                        ? $"Your ticket ID is {inquiry.TicketId}. Use this to track further progress."
                        : "Contact support if you haven't received ticket information."
                }
            ),
            "Rejected" => (
                $"Your inquiry '{inquiry.Title}' has been rejected.",
                new List<string> { "Contact support for more information about the rejection reason." }
            ),
            _ => (
                $"Inquiry '{inquiry.Title}' status: {status}",
                new List<string> { "Contact support if you need assistance." }
            )
        };

        var response = new InquiryStatusResponse
        {
            Id = inquiry.Id,
            Title = inquiry.Title,
            Description = inquiry.Description,
            Status = status,
            StatusDescription = statusDescription,
            Category = inquiry.Category.ToString(),
            CreatedAt = inquiry.CreatedAt,
            TicketId = inquiry.TicketId,
            HasTicket = !string.IsNullOrEmpty(inquiry.TicketId),
            Summary = summary,
            NextSteps = nextSteps
        };

        return response.ToJson();
    }

    private static string GetStatusDescription(string status) => status switch
    {
        "Submitted" => "Inquiry received and awaiting processing",
        "Processing" => "Inquiry is being processed",
        "Completed" => "Inquiry completed - ticket created",
        "Rejected" => "Inquiry was rejected",
        _ => "Unknown status"
    };
}
