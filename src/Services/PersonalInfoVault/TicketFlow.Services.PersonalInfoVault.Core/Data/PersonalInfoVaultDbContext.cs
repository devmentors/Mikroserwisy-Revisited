using Microsoft.EntityFrameworkCore;
using TicketFlow.Services.PersonalInfoVault.Core.Data.Configurations;
using TicketFlow.Services.PersonalInfoVault.Core.Data.Models;

namespace TicketFlow.Services.PersonalInfoVault.Core.Data;

public class PersonalInfoVaultDbContext : DbContext
{
    public DbSet<PersonalInfo> PersonalInfos { get; set; }

    public PersonalInfoVaultDbContext(DbContextOptions<PersonalInfoVaultDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PersonalInfoConfiguration());
    }
}
