using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Application.Common.Security;

public class ClassAccessService(IApplicationDbContext db, ICurrentUser currentUser) : IClassAccessService
{
    public IQueryable<int>? AccessibleClassIds()
    {
        if (currentUser.IsAdmin)
        {
            return null;
        }

        var teacherId = currentUser.Id;

        // Classes where she teaches at least one subject, plus the classes where she is the homeroom teacher.
        return db.TeacherAssignments
            .Where(a => a.TeacherId == teacherId)
            .Select(a => a.ClassId)
            .Union(db.Classes
                .Where(c => c.HomeroomTeacherId == teacherId)
                .Select(c => c.Id));
    }

    public async Task EnsureCanAccessClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        var accessibleClassIds = AccessibleClassIds();
        if (accessibleClassIds is null)
        {
            return;
        }

        if (!await accessibleClassIds.ContainsAsync(classId, cancellationToken))
        {
            throw new ForbiddenException("You do not teach in this class.");
        }
    }
}
