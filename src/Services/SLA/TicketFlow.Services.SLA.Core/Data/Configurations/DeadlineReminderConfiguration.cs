using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Services.SLA.Core.Data.Models;

namespace TicketFlow.Services.SLA.Core.Data.Configurations;

public class DeadlineReminderConfiguration : IEntityTypeConfiguration<DeadlineReminders>
{

    public void Configure(EntityTypeBuilder<DeadlineReminders> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceType).IsRequired();
        builder.Property(x => x.ServiceSourceId).IsRequired();
        builder.Property(x => x.UserIdToRemind);
        builder.Property(x => x.FirstReminderDateUtc);
        builder.Property(x => x.FirstReminderSent);
        builder.Property(x => x.SecondReminderDateUtc);
        builder.Property(x => x.SecondReminderSent);
        builder.Property(x => x.FinalReminderDateUtc);
        builder.Property(x => x.FinalReminderSent);
        builder.Property(x => x.DeadlineMet);
        builder.Property(x => x.DeadlineDateUtc).IsRequired();
        builder.Property(x => x.LastDeadlineBreachedAlertSentDateUtc);
        
    }
}