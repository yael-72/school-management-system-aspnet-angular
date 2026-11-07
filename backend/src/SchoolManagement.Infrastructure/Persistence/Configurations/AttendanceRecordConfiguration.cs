using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("Attendance");

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Note).HasMaxLength(500);

        // Business rule: one attendance record per student per day.
        builder.HasIndex(a => new { a.StudentId, a.Date }).IsUnique();

        // Dashboard queries filter by date ("today's attendance"), so index it too.
        builder.HasIndex(a => a.Date);

        builder.HasOne(a => a.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.RecordedBy)
            .WithMany()
            .HasForeignKey(a => a.RecordedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
