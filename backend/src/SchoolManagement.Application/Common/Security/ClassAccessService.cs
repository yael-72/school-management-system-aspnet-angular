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

    public async Task<IReadOnlySet<ClassSubject>?> GetGradableSubjectsAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return null;
        }

        var teacherId = currentUser.Id;
        var pairs = await db.TeacherAssignments
            .Where(a => a.TeacherId == teacherId)
            .Select(a => new { a.ClassId, a.SubjectId })
            .ToListAsync(cancellationToken);

        return pairs.Select(p => new ClassSubject(p.ClassId, p.SubjectId)).ToHashSet();
    }

    public async Task EnsureCanGradeAsync(int classId, int subjectId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return;
        }

        var teacherId = currentUser.Id;
        var teachesSubject = await db.TeacherAssignments.AnyAsync(
            a => a.TeacherId == teacherId && a.ClassId == classId && a.SubjectId == subjectId,
            cancellationToken);

        if (!teachesSubject)
        {
            throw new ForbiddenException("You do not teach this subject in this class.");
        }
    }
}
