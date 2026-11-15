using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments", t =>
        {
            t.HasCheckConstraint("CK_Assessments_Type", "[Type] IN (N'Exam', N'Quiz', N'Assignment')");
            t.HasCheckConstraint("CK_Assessments_MaxScore", "[MaxScore] > 0");
        });

        builder.Property(a => a.Title).HasMaxLength(100).IsRequired();
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSDATETIME()");

        // Store the enum as text ("Exam") instead of a number, so the table is readable.
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(a => new { a.ClassId, a.Date }).HasDatabaseName("IX_Assessments_Class_Date");

        builder.HasOne(a => a.Class)
            .WithMany(c => c.Assessments)
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Subject)
            .WithMany(s => s.Assessments)
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
