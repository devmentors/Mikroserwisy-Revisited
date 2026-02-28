using Microsoft.Extensions.Logging;
using TicketFlow.Services.Inquiries.Core.Data;
using TicketFlow.Shared.Caching;
using Microsoft.EntityFrameworkCore;

namespace TicketFlow.Services.Inquiries.Core.Statistics;

public interface IInquiryStatisticsService
{
    Task<InquiryStatistics> GetStatisticsAsync(CancellationToken ct = default);
    Task InvalidateCacheAsync(CancellationToken ct = default);
}

public sealed record InquiryStatistics(
    int TotalInquiries,
    int PendingInquiries,
    int ProcessedInquiries,
    DateTime CachedAt);

internal sealed class InquiryStatisticsService(
    InquiriesDbContext dbContext,
    ICacheService cacheService,
    ILogger<InquiryStatisticsService> logger) : IInquiryStatisticsService
{
    private const string CacheKey = "statistics:summary";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    public async Task<InquiryStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var cached = await cacheService.GetAsync<InquiryStatistics>(CacheKey, ct);
        if (cached is not null)
        {
            logger.LogInformation(
                "Statistics cache hit. Total: {Total}, Pending: {Pending}, Processed: {Processed}",
                cached.TotalInquiries,
                cached.PendingInquiries,
                cached.ProcessedInquiries);
            return cached;
        }

        logger.LogInformation("Statistics cache miss. Computing from database...");

        var total = await dbContext.Inquiries.CountAsync(ct);
        var pending = await dbContext.Inquiries
            .Where(i => i.Status == Data.Models.InquiryStatus.New)
            .CountAsync(ct);
        var processed = await dbContext.Inquiries
            .Where(i => i.Status == Data.Models.InquiryStatus.Resolved || i.Status == Data.Models.InquiryStatus.Closed)
            .CountAsync(ct);

        var stats = new InquiryStatistics(
            TotalInquiries: total,
            PendingInquiries: pending,
            ProcessedInquiries: processed,
            CachedAt: DateTime.UtcNow);

        await cacheService.SetAsync(CacheKey, stats, CacheExpiry, ct);

        logger.LogInformation(
            "Statistics computed and cached. Total: {Total}, Pending: {Pending}, Processed: {Processed}",
            stats.TotalInquiries,
            stats.PendingInquiries,
            stats.ProcessedInquiries);

        return stats;
    }

    public async Task InvalidateCacheAsync(CancellationToken ct = default)
    {
        await cacheService.RemoveAsync(CacheKey, ct);
        logger.LogInformation("Statistics cache invalidated");
    }
}
