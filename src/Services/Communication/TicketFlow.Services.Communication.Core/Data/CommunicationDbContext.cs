using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Communication.Core.Data.Models;

namespace TicketFlow.Services.Communication.Core.Data;

public class CommunicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Message> Messages { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.HasDefaultSchema("communication");
    }
}