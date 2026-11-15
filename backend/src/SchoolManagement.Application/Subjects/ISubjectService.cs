namespace SchoolManagement.Application.Subjects;

public interface ISubjectService
{
    /// <summary>All subjects, alphabetically. Not paged: a school has a handful of subjects.</summary>
    Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default);

    Task<SubjectDto> CreateSubjectAsync(SaveSubjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>Renames a subject; its assignments and assessments keep pointing to it.</summary>
    Task<SubjectDto> UpdateSubjectAsync(int id, SaveSubjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a subject that is not taught and has no assessments.</summary>
    Task DeleteSubjectAsync(int id, CancellationToken cancellationToken = default);
}
