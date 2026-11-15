using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.ToTable("UploadedFiles");
        builder.Property(f => f.FileName).HasMaxLength(200).IsRequired();
        builder.Property(f => f.StoredPath).HasMaxLength(400).IsRequired();
        builder.Property(f => f.UploadedAt).HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(f => f.User)
            .WithMany(u => u.UploadedFiles)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
