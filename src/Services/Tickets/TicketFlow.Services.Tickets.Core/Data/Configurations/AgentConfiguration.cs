using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Data.Configurations;

internal sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.HasKey(x => x.Id);
        builder
            .HasMany(x => x.Tickets)
            .WithOne(x => x.AssignedAgent)
            .HasForeignKey(x => x.AssignedTo);
        builder.Property(x => x.FullName).IsRequired();
    }
}