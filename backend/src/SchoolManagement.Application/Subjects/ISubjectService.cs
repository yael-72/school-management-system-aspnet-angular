namespace SchoolManagement.Application.Subjects;

public interface ISubjectService
{
    /// <summary>All subjects, alphabetically. Not paged: a school has a handful of subjects.</summary>
    Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default);
}
