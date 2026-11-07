using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("Classes", t =>
            t.HasCheckConstraint("CK_Classes_GradeLevel", "[GradeLevel] BETWEEN 1 AND 12"));

        builder.Property(c => c.Name).HasMaxLength(20).IsRequired();
        builder.Property(c => c.SchoolYear).HasMaxLength(10).IsRequired();

        // The same class name can exist again in a different school year.
        builder.HasIndex(c => new { c.Name, c.SchoolYear }).IsUnique().HasDatabaseName("UQ_Classes_Name_Year");

        // If a homeroom teacher is deleted, the class stays and simply has no homeroom teacher.
        builder.HasOne(c => c.HomeroomTeacher)
            .WithMany(u => u.HomeroomClasses)
            .HasForeignKey(c => c.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
