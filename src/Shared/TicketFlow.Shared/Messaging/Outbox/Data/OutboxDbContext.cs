using Microsoft.EntityFrameworkCore;

namespace TicketFlow.Shared.Messaging.Outbox.Data;

public sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("outbox");
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}