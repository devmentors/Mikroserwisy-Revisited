using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.Anonymization.Core.Data.Models;

namespace TicketFlow.Services.Anonymization.Core.Data.Configurations;

public class AnonymizationRequestConfiguration : IEntityTypeConfiguration<AnonymizationRequest>
{
    public void Configure(EntityTypeBuilder<AnonymizationRequest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PersonToken).IsRequired().HasMaxLength(100);
        builder.Property(x => x.RequestedByEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.PersonToken);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.ServiceStatuses)
            .WithOne(x => x.AnonymizationRequest)
            .HasForeignKey(x => x.AnonymizationRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ServiceAnonymizationStatusConfiguration : IEntityTypeConfiguration<ServiceAnonymizationStatus>
{
    public void Configure(EntityTypeBuilder<ServiceAnonymizationStatus> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
    }
}
