using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Property(r => r.Name).HasColumnName("Role").HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();

        // Roles are fixed reference data, so they are part of the migration itself (HasData)
        // and exist in every environment, not only in development.
        builder.HasData(
            new Role { Id = RoleIds.Admin, Name = RoleNames.Admin },
            new Role { Id = RoleIds.Teacher, Name = RoleNames.Teacher });
    }
}
