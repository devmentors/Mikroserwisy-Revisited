using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.SLA.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Data.Configurations;

internal sealed class SignedSLAConfiguration : IEntityTypeConfiguration<SignedSLA>
{
    private static readonly JsonSerializerOptions JsonOpt = new JsonSerializerOptions
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    
    public void Configure(EntityTypeBuilder<SignedSLA> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).IsRequired();
        builder.Property(x => x.Domain).IsRequired();
        builder.Property(x => x.ClientTier).IsRequired();
        builder.Property<Dictionary<ServiceType, SLADeadlines>>("_agreedResponseDeadlines")
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(x => JsonSerializer.Serialize(x, JsonOpt),
                x => JsonSerializer.Deserialize<Dictionary<ServiceType, SLADeadlines>>(x, JsonOpt));
    }
}