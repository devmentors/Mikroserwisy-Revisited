using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.Communication.Core.Data.Models;

namespace TicketFlow.Services.Communication.Core.Data.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{

    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsRead).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        
    }
}