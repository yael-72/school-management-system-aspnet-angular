using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Assessments;

public interface IAssessmentService
{
    /// <summary>Assessments of the classes the current user may access, filtered, sorted and paged.</summary>
    Task<PagedResult<AssessmentListItemDto>> GetAssessmentsAsync(AssessmentListQuery query, CancellationToken cancellationToken = default);

    Task<AssessmentDto> GetAssessmentAsync(int id, CancellationToken cancellationToken = default);

    Task<AssessmentDto> CreateAssessmentAsync(SaveAssessmentRequest request, CancellationToken cancellationToken = default);

    Task<AssessmentDto> UpdateAssessmentAsync(int id, SaveAssessmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the assessment together with its scores.</summary>
    Task DeleteAssessmentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The grade sheet: every active student of the class with their score, plus simple statistics.</summary>
    Task<AssessmentScoresDto> GetScoresAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the scores of the class on this assessment. Existing scores are updated and missing ones
    /// are created (upsert), so saving twice never creates duplicates.
    /// </summary>
    Task<AssessmentScoresDto> SaveScoresAsync(int id, SaveScoresRequest request, CancellationToken cancellationToken = default);
}
