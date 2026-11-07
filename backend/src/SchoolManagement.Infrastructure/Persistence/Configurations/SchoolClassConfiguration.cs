using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("Classes", t =>
            t.HasCheckConstraint("CK_Classes_GradeLevel", "[GradeLevel] BETWEEN 7 AND 12"));

        builder.Property(c => c.Name).HasMaxLength(20).IsRequired();
        builder.Property(c => c.SchoolYear).HasMaxLength(9).IsRequired();

        // The same class name can exist again in a different school year.
        builder.HasIndex(c => new { c.Name, c.SchoolYear }).IsUnique();

        builder.HasOne(c => c.HomeroomTeacher)
            .WithMany(u => u.HomeroomClasses)
            .HasForeignKey(c => c.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
