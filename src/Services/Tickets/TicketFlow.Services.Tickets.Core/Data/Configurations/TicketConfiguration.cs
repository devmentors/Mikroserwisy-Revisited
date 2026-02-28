using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Data.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PersonToken).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.PersonToken);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.LanguageCode).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.Type).HasConversion(x => x.ToString(), x => Enum.Parse<TicketType>(x));
        builder.Property(x => x.Status).HasConversion(x => x.ToString(), x => Enum.Parse<TicketStatus>(x));
        builder.Property(x => x.Resolution).IsRequired(false);
    }
}