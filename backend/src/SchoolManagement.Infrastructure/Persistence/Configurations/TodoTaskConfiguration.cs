using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class TodoTaskConfiguration : IEntityTypeConfiguration<TodoTask>
{
    public void Configure(EntityTypeBuilder<TodoTask> builder)
    {
        builder.ToTable("TodoTasks");
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("SYSDATETIME()");

        // A to-do list is personal data: it is removed together with its user.
        builder.HasOne(t => t.User)
            .WithMany(u => u.TodoTasks)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
