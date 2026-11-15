using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Classes;

public interface IClassService
{
    Task<PagedResult<ClassSummaryDto>> GetClassesAsync(ClassListQuery query, CancellationToken cancellationToken = default);

    Task<ClassDetailsDto> GetClassAsync(int id, CancellationToken cancellationToken = default);

    Task<ClassDetailsDto> CreateClassAsync(SaveClassRequest request, CancellationToken cancellationToken = default);

    Task<ClassDetailsDto> UpdateClassAsync(int id, SaveClassRequest request, CancellationToken cancellationToken = default);

    Task DeleteClassAsync(int id, CancellationToken cancellationToken = default);
}
