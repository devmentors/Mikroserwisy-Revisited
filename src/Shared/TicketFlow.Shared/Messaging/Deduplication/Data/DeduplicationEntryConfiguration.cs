using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TicketFlow.Shared.Messaging.Deduplication.Data;

internal sealed class DeduplicationEntryConfiguration : IEntityTypeConfiguration<DeduplicationEntry>
{
    public void Configure(EntityTypeBuilder<DeduplicationEntry> builder)
    {
        builder.ToTable("DeduplicationEntries");
        builder.HasKey(x => x.MessageId);
    }
}