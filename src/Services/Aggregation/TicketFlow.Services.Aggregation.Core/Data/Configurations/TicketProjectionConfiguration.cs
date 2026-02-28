using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.Aggregation.Core.Models;

namespace TicketFlow.Services.Aggregation.Core.Data.Configurations;

public class TicketProjectionConfiguration : IEntityTypeConfiguration<TicketProjection>
{
    public void Configure(EntityTypeBuilder<TicketProjection> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PersonToken).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.PersonToken);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AgentName).HasMaxLength(200);
        builder.Property(x => x.AgentAvatarUrl).HasMaxLength(500);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SlaBreached);
        builder.HasIndex(x => x.CreatedAt);
    }
}
