using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("Attendance", t =>
            t.HasCheckConstraint("CK_Attendance_Status", "[Status] IN (N'Present', N'Absent', N'Late', N'Excused')"));

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(10);
        builder.Property(a => a.Note).HasMaxLength(200);

        // Business rule: one attendance record per student per day.
        builder.HasIndex(a => new { a.StudentId, a.Date }).IsUnique().HasDatabaseName("UQ_Attendance");

        // Dashboard queries filter by date ("today's attendance"), so index it too.
        builder.HasIndex(a => a.Date).HasDatabaseName("IX_Attendance_Date");

        builder.HasOne(a => a.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.RecordedByUser)
            .WithMany()
            .HasForeignKey(a => a.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
