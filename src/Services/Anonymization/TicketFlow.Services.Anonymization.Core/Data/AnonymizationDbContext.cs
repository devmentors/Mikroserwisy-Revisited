using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Anonymization.Core.Data.Models;

namespace TicketFlow.Services.Anonymization.Core.Data;

public class AnonymizationDbContext : DbContext
{
    public DbSet<AnonymizationRequest> AnonymizationRequests { get; set; }
    public DbSet<ServiceAnonymizationStatus> ServiceStatuses { get; set; }

    public AnonymizationDbContext(DbContextOptions<AnonymizationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnonymizationDbContext).Assembly);
    }
}
