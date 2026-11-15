using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.TeacherAssignments;

/// <summary>
/// Who teaches which subject in which class. These rows give teachers their access (see
/// IClassAccessService), so managing them is admin-only.
/// </summary>
public interface ITeacherAssignmentService
{
    Task<PagedResult<TeacherAssignmentDto>> GetAssignmentsAsync(TeacherAssignmentListQuery query, CancellationToken cancellationToken = default);

    Task<TeacherAssignmentDto> CreateAssignmentAsync(CreateTeacherAssignmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the assignment. The teacher loses access to the class (unless she is its homeroom
    /// teacher or teaches another subject there); assessments she created stay.
    /// </summary>
    Task DeleteAssignmentAsync(int id, CancellationToken cancellationToken = default);
}
