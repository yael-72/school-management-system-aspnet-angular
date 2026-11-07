using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class ScoreConfiguration : IEntityTypeConfiguration<Score>
{
    public void Configure(EntityTypeBuilder<Score> builder)
    {
        // The upper limit (MaxScore) lives in another table, so it is checked in the
        // application service; the database still guards against negative values.
        builder.ToTable("Scores", t =>
            t.HasCheckConstraint("CK_Scores_Value", "[Value] >= 0"));

        builder.Property(s => s.Comment).HasMaxLength(500);

        // Business rule: one score per student per assessment.
        builder.HasIndex(s => new { s.AssessmentId, s.StudentId }).IsUnique();

        builder.HasOne(s => s.Assessment)
            .WithMany(a => a.Scores)
            .HasForeignKey(s => s.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict (not Cascade) because SQL Server rejects two cascade paths
        // from Classes to Scores (via Students and via Assessments).
        builder.HasOne(s => s.Student)
            .WithMany(st => st.Scores)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
