using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable("NotificationSettings");

        // One-to-one with User: the user id is both the primary key and the foreign key.
        builder.HasKey(n => n.UserId);

        builder.HasOne(n => n.User)
            .WithOne(u => u.NotificationSettings)
            .HasForeignKey<NotificationSettings>(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
