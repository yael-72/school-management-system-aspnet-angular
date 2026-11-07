using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.Property(s => s.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(s => s.LastName).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ParentPhone).HasMaxLength(20);
        builder.Property(s => s.Address).HasMaxLength(200);
        builder.Ignore(s => s.FullName);

        // Most queries are "active students of class X", so the index covers both columns.
        builder.HasIndex(s => new { s.ClassId, s.IsActive }).HasDatabaseName("IX_Students_ClassId");

        // Restrict: a class with students cannot be deleted by accident (business rule).
        builder.HasOne(s => s.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
