using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.Inquiries.Core.Data.Models;

namespace TicketFlow.Services.Inquiries.Core.Data;

public sealed class InquiriesDbContext(DbContextOptions<InquiriesDbContext> options) : DbContext(options)
{
    public DbSet<Inquiry> Inquiries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}