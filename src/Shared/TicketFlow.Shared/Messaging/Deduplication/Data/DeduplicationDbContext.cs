using Microsoft.EntityFrameworkCore;

namespace TicketFlow.Shared.Messaging.Deduplication.Data;

public sealed class DeduplicationDbContext(DbContextOptions<DeduplicationDbContext> options) : DbContext(options)
{
    public DbSet<DeduplicationEntry> DeduplicationEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("deduplication");
        modelBuilder.ApplyConfiguration(new DeduplicationEntryConfiguration());
    }
}