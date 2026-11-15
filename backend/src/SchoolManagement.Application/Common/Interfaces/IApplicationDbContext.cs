using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Common.Interfaces;

/// <summary>
/// The database as the application services see it. Services depend on this interface
/// (implemented by AppDbContext in Infrastructure), so the Application layer does not
/// reference the Infrastructure project, and tests can use any EF Core provider.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<SchoolClass> Classes { get; }
    DbSet<Student> Students { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<TeacherAssignment> TeacherAssignments { get; }
    DbSet<Assessment> Assessments { get; }
    DbSet<Score> Scores { get; }
    DbSet<AttendanceRecord> Attendance { get; }
    DbSet<SchoolEvent> Events { get; }
    DbSet<TodoTask> TodoTasks { get; }
    DbSet<UploadedFile> UploadedFiles { get; }
    DbSet<NotificationSettings> NotificationSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
