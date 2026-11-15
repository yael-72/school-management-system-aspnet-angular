namespace SchoolManagement.Application.Common.Security;

/// <summary>
/// Answers "which classes may the current user work with?".
/// Admins may access every class. A teacher may access a class only when she teaches a subject
/// in it (TeacherAssignments) or is its homeroom teacher. The role alone does not give access.
///
/// Grading is narrower than access: a teacher may create assessments and enter scores only for a
/// subject she teaches in that class. A homeroom teacher can see all the grades of her class, but
/// edit only her own subjects.
///
/// Every feature that touches class data (classes, students, attendance, grades) uses this one
/// service, so the permission rules live in a single place.
/// </summary>
public interface IClassAccessService
{
    /// <summary>
    /// The ids of the classes the current user may access, as a query that EF Core can use inside
    /// other queries (it becomes a SQL sub-query). Null for admins, meaning "no restriction".
    /// </summary>
    IQueryable<int>? AccessibleClassIds();

    /// <summary>Throws ForbiddenException when the current user may not access the class.</summary>
    Task EnsureCanAccessClassAsync(int classId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The (class, subject) pairs the current user may grade. Null for admins, meaning "every pair".
    /// Loaded once per request, so a list can mark each row as editable without extra queries.
    /// </summary>
    Task<IReadOnlySet<ClassSubject>?> GetGradableSubjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws ForbiddenException when the current user may not grade this subject in this class.</summary>
    Task EnsureCanGradeAsync(int classId, int subjectId, CancellationToken cancellationToken = default);
}

/// <summary>A subject in a class – the unit a teacher is assigned to and grades.</summary>
public readonly record struct ClassSubject(int ClassId, int SubjectId);
