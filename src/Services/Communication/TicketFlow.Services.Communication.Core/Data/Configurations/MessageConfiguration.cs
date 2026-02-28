using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.Communication.Core.Data.Models;

namespace TicketFlow.Services.Communication.Core.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{

    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipentEmail);
        builder.Property(x => x.RecipentUserId);
        builder.Property(x => x.SenderUserId); // If NULL -> sender == SYSTEM
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Timestamp).IsRequired();
        builder.Property(x => x.IsRead).IsRequired();
        
    }
}