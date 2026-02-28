using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.Tickets.Core.Data.Models;

namespace TicketFlow.Services.Tickets.Core.Data.Configurations;

public class TicketScheduledActionConfiguration : IEntityTypeConfiguration<TicketScheduledAction>
{
    public void Configure(EntityTypeBuilder<TicketScheduledAction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TranslatedText).IsRequired();
    }
}