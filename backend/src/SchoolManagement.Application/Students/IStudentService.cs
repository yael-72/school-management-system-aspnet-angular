using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Students;

public interface IStudentService
{
    Task<PagedResult<StudentListItemDto>> GetStudentsAsync(StudentListQuery query, CancellationToken cancellationToken = default);

    Task<StudentDto> GetStudentAsync(int id, CancellationToken cancellationToken = default);

    Task<StudentSummaryDto> GetSummaryAsync(int id, CancellationToken cancellationToken = default);

    Task<StudentDto> CreateStudentAsync(SaveStudentRequest request, CancellationToken cancellationToken = default);

    Task<StudentDto> UpdateStudentAsync(int id, SaveStudentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: marks the student inactive so grade and attendance history is kept.</summary>
    Task DeleteStudentAsync(int id, CancellationToken cancellationToken = default);
}
