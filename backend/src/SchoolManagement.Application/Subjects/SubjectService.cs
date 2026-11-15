using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Application.Subjects;

/// <summary>Subjects are school-wide reference data, so every logged-in user may read them.</summary>
public class SubjectService(IApplicationDbContext db) : ISubjectService
{
    public async Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default) =>
        await db.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Select(s => new SubjectDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);
}
