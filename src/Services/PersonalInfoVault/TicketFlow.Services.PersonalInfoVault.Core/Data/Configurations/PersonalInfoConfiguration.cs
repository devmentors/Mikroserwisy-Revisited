using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.PersonalInfoVault.Core.Data.Models;

namespace TicketFlow.Services.PersonalInfoVault.Core.Data.Configurations;

public class PersonalInfoConfiguration : IEntityTypeConfiguration<PersonalInfo>
{
    public void Configure(EntityTypeBuilder<PersonalInfo> builder)
    {
        builder.ToTable("PersonalInfos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PersonToken).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.PersonToken).IsUnique();
        builder.HasIndex(x => x.Email);
    }
}
