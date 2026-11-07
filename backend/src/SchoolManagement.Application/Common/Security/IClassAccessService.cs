namespace SchoolManagement.Application.Common.Security;

/// <summary>
/// Answers "which classes may the current user work with?".
/// Admins may access every class. A teacher may access a class only when she teaches a subject
/// in it (TeacherAssignments) or is its homeroom teacher. The role alone does not give access.
///
/// Every feature that touches class data (classes, students, attendance, grades) uses this one
/// service, so the permission rule lives in a single place.
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
}
