using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class SchoolEventConfiguration : IEntityTypeConfiguration<SchoolEvent>
{
    public void Configure(EntityTypeBuilder<SchoolEvent> builder)
    {
        builder.ToTable("Events", t =>
            t.HasCheckConstraint("CK_Events_EndAfterStart", "[EndDate] IS NULL OR [EndDate] >= [StartDate]"));

        builder.Property(e => e.Title).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => e.StartDate);

        builder.HasOne(e => e.Class)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
