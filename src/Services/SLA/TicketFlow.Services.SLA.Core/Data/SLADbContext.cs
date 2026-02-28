using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.SLA.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Data;

public class SLADbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<SignedSLA> SignedSLAs { get; set; }
    public DbSet<DeadlineReminders> DeadlineReminders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.HasDefaultSchema("sla");
    }
}