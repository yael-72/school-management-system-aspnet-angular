using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Empties the database for the live demo, so the seeder can build a fresh school on startup.
/// That undoes whatever visitors changed and keeps the demo dates around "today"
/// (the dashboard shows this week, the last 30 days, and so on).
/// </summary>
public class DemoDataReset(AppDbContext context, IFileStorage fileStorage, ILogger<DemoDataReset> logger)
{
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // Uploaded file contents first, while the rows still say where they are.
        var storedPaths = await context.UploadedFiles.Select(f => f.StoredPath).ToListAsync(cancellationToken);
        foreach (var storedPath in storedPaths)
        {
            fileStorage.Delete(storedPath);
        }

        // One DELETE statement per table, children before parents (most foreign keys are
        // ON DELETE RESTRICT). Roles are fixed reference data and stay. There is no transaction:
        // if the app stops halfway, the next start simply clears the rest.
        await context.Scores.ExecuteDeleteAsync(cancellationToken);
        await context.Attendance.ExecuteDeleteAsync(cancellationToken);
        await context.Assessments.ExecuteDeleteAsync(cancellationToken);
        await context.Events.ExecuteDeleteAsync(cancellationToken);
        await context.TeacherAssignments.ExecuteDeleteAsync(cancellationToken);
        await context.Students.ExecuteDeleteAsync(cancellationToken);
        await context.Classes.ExecuteDeleteAsync(cancellationToken);
        await context.Subjects.ExecuteDeleteAsync(cancellationToken);
        await context.TodoTasks.ExecuteDeleteAsync(cancellationToken);
        await context.UploadedFiles.ExecuteDeleteAsync(cancellationToken);
        await context.NotificationSettings.ExecuteDeleteAsync(cancellationToken);
        await context.Users.ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Demo data cleared ({FileCount} uploaded files deleted)", storedPaths.Count);
    }
}
