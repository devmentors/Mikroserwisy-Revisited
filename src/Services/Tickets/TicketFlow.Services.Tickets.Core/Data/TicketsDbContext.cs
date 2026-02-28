using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Data;

public sealed class TicketsDbContext(DbContextOptions<TicketsDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketScheduledAction> TicketScheduledActions { get; set; }
    public DbSet<Agent> Agents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.HasDefaultSchema("tickets");
    }
}