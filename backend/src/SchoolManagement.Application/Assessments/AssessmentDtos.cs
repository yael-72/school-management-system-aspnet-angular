using System.ComponentModel.DataAnnotations;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Assessments;

/// <summary>GET /api/assessments?classId=3&amp;subjectId=1&amp;from=2026-09-01&amp;to=2026-12-31&amp;type=Exam</summary>
public class AssessmentListQuery : ListQuery
{
    [Range(1, int.MaxValue)]
    public int? ClassId { get; init; }

    [Range(1, int.MaxValue)]
    public int? SubjectId { get; init; }

    [EnumDataType(typeof(AssessmentType), ErrorMessage = "Type is not valid.")]
    public AssessmentType? Type { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

/// <summary>One row of the assessments table.</summary>
/// <remarks>
/// Init properties (not a positional record) because the list query sorts on these properties –
/// see the note on StudentListItemDto.
/// </remarks>
public record AssessmentListItemDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public AssessmentType Type { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public int SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public decimal MaxScore { get; init; }

    /// <summary>How many active students of the class have a score (not null).</summary>
    public int GradedCount { get; init; }

    /// <summary>How many active students the class has.</summary>
    public int StudentCount { get; init; }

    /// <summary>Average of the graded scores, as a percentage (0–100).</summary>
    public decimal? AverageScore { get; init; }

    /// <summary>Whether the current user may edit this assessment and its scores.</summary>
    public bool CanEdit { get; init; }
}

/// <summary>GET /api/assessments/{id}</summary>
public record AssessmentDto(
    int Id,
    string Title,
    AssessmentType Type,
    int ClassId,
    string ClassName,
    int SubjectId,
    string SubjectName,
    DateOnly Date,
    decimal MaxScore,
    string CreatedByName,
    bool CanEdit);

/// <summary>Body of POST /api/assessments and PUT /api/assessments/{id}.</summary>
public record SaveAssessmentRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(100, ErrorMessage = "Title can be at most 100 characters.")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Type is required.")]
    [EnumDataType(typeof(AssessmentType), ErrorMessage = "Type is not valid.")]
    public AssessmentType? Type { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Class is required.")]
    public int ClassId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Subject is required.")]
    public int SubjectId { get; init; }

    [Required(ErrorMessage = "Date is required.")]
    public DateOnly? Date { get; init; }

    // The column is decimal(5,2), so 999 is the largest value that fits.
    [Range(typeof(decimal), "1", "999", ErrorMessage = "Max score must be between 1 and 999.")]
    public decimal MaxScore { get; init; } = 100;
}

/// <summary>One row of the grade sheet. Score is null when the student was not graded yet.</summary>
public record StudentScoreDto(
    int StudentId,
    string FirstName,
    string LastName,
    decimal? Score,
    decimal? Percent,
    string? Comment);

/// <summary>
/// GET /api/assessments/{id}/scores – every active student of the class with their score.
/// Not paged: a class has a few dozen students at most.
/// </summary>
public record AssessmentScoresDto(
    AssessmentDto Assessment,
    int GradedCount,
    decimal? AverageScore,
    decimal? HighestScore,
    decimal? LowestScore,
    IReadOnlyList<StudentScoreDto> Students);

/// <summary>Body of PUT /api/assessments/{id}/scores: the scores of the class on this assessment.</summary>
public record SaveScoresRequest
{
    [Required(ErrorMessage = "At least one score is required.")]
    [MinLength(1, ErrorMessage = "At least one score is required.")]
    public IReadOnlyList<SaveScoreEntry> Scores { get; init; } = [];
}

/// <summary>
/// A score of null with no comment clears the student's score. The upper limit (the assessment's
/// MaxScore) depends on the database, so the service checks it.
/// </summary>
public record SaveScoreEntry
{
    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; init; }

    [Range(typeof(decimal), "0", "999", ErrorMessage = "Score must be between 0 and 999.")]
    public decimal? Score { get; init; }

    [MaxLength(300, ErrorMessage = "Comment can be at most 300 characters.")]
    public string? Comment { get; init; }
}
