using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Score> Scores => Set<Score>();
    public DbSet<AttendanceRecord> Attendance => Set<AttendanceRecord>();
    public DbSet<SchoolEvent> Events => Set<SchoolEvent>();
    public DbSet<TodoTask> TodoTasks => Set<TodoTask>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each entity has its own IEntityTypeConfiguration class in the Configurations folder.
        // This line finds and applies all of them, which keeps this method short.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // A sensible default for every decimal column (scores, max scores), instead of
        // repeating HasPrecision on each property.
        configurationBuilder.Properties<decimal>().HavePrecision(5, 2);
    }
}
