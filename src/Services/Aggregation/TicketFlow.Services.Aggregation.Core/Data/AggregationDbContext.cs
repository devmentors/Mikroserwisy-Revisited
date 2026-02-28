using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Aggregation.Core.Models;

namespace TicketFlow.Services.Aggregation.Core.Data;

public class AggregationDbContext(DbContextOptions<AggregationDbContext> options) : DbContext(options)
{
    public DbSet<TicketProjection> TicketProjections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.HasDefaultSchema("aggregation");
    }
}
