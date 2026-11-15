using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("TeacherAssignments");

        // The same teacher cannot be assigned twice to the same subject in the same class.
        builder.HasIndex(a => new { a.TeacherId, a.ClassId, a.SubjectId })
            .IsUnique()
            .HasDatabaseName("UQ_TeacherAssignment");

        // Deleting a class or subject removes its assignments; deleting a teacher who still
        // has assignments is blocked so it is a deliberate action.
        builder.HasOne(a => a.Teacher)
            .WithMany(u => u.TeacherAssignments)
            .HasForeignKey(a => a.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Class)
            .WithMany(c => c.TeacherAssignments)
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Subject)
            .WithMany(s => s.TeacherAssignments)
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
