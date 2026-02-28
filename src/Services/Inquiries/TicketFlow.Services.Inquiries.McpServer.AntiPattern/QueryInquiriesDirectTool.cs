using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Npgsql;

namespace TicketFlow.Services.Inquiries.McpServer.AntiPattern;

public sealed class QueryInquiriesDirectTool
{
    private readonly DatabaseSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<QueryInquiriesDirectTool> _logger;

    public QueryInquiriesDirectTool(
        DatabaseSettings settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<QueryInquiriesDirectTool> logger)
    {
        _settings = settings;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [McpServerTool(Name = "query_inquiries_direct")]
    [Description("Query inquiries directly from database with flexible SQL filtering.")]
    public async Task<string> ExecuteAsync(
        [Description("Maximum number of results (default 100)")]
        int limit = 100,
        [Description("Optional status filter (New, Submitted, Processing, Completed, Rejected)")]
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        var userId = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
        _logger.LogWarning("User {UserId} is querying ALL inquiries - no user scoping applied", userId ?? "UNKNOWN");

        await using var connection = new NpgsqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(ct);

        var sql = """
            SELECT "Id", "UserId", "PersonToken", "Title", "Description",
                   "CreatedAt", "Status", "Category", "TicketId"
            FROM "Inquiries"
            WHERE 1=1
            """;

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var validStatuses = new[] { "0", "1", "2", "3", "4", "New", "Submitted", "Processing", "Completed", "Rejected" };
            if (!validStatuses.Contains(statusFilter, StringComparer.OrdinalIgnoreCase))
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Invalid status filter",
                    validValues = validStatuses
                });
            }

            var statusValue = statusFilter.ToLower() switch
            {
                "new" => 0,
                "submitted" => 1,
                "processing" => 2,
                "completed" => 3,
                "rejected" => 4,
                _ when int.TryParse(statusFilter, out var v) => v,
                _ => -1
            };

            sql += $""" AND "Status" = {statusValue}""";
        }

        sql += $""" ORDER BY "CreatedAt" DESC LIMIT {Math.Min(limit, 1000)}""";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var results = new List<object>();
        while (await reader.ReadAsync(ct))
        {
            results.Add(new
            {
                id = reader.GetGuid(0),
                userId = reader.IsDBNull(1) ? null : reader.GetGuid(1).ToString(),
                personToken = reader.GetString(2),
                title = reader.GetString(3),
                description = reader.GetString(4),
                createdAt = reader.GetDateTime(5),
                status = reader.GetInt32(6),
                category = reader.GetInt32(7),
                ticketId = reader.IsDBNull(8) ? null : reader.GetGuid(8).ToString()
            });
        }

        return JsonSerializer.Serialize(new
        {
            message = $"Found {results.Count} inquiries",
            data = results
        });
    }
}
