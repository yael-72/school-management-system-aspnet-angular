using System.ComponentModel.DataAnnotations;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Classes;

/// <summary>GET /api/classes?gradeLevel=7&amp;search=...&amp;sortBy=name</summary>
public class ClassListQuery : ListQuery
{
    /// <summary>Only classes of this grade level (the tabs on the classes page).</summary>
    [Range(1, 12)]
    public byte? GradeLevel { get; init; }
}

/// <summary>One class card on the classes page.</summary>
/// <remarks>
/// Uses init properties (not a positional record) because list queries sort on these properties:
/// EF Core can translate ORDER BY on a DTO built with an object initializer, but not on one built
/// through a constructor.
/// </remarks>
public record ClassSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public byte GradeLevel { get; init; }
    public string SchoolYear { get; init; } = string.Empty;
    public int? HomeroomTeacherId { get; init; }
    public string? HomeroomTeacherName { get; init; }
    public int StudentCount { get; init; }

    /// <summary>Average of all graded scores of the class's active students, as a percentage (0–100).</summary>
    public decimal? AverageScore { get; init; }
}

/// <summary>A subject taught in a class, and who teaches it.</summary>
public record ClassSubjectDto(int SubjectId, string SubjectName, int TeacherId, string TeacherName);

public record ClassDetailsDto(
    int Id,
    string Name,
    byte GradeLevel,
    string SchoolYear,
    int? HomeroomTeacherId,
    string? HomeroomTeacherName,
    int StudentCount,
    decimal? AverageScore,
    IReadOnlyList<ClassSubjectDto> Subjects);

/// <summary>Body of POST /api/classes and PUT /api/classes/{id}.</summary>
public record SaveClassRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(20, ErrorMessage = "Name can be at most 20 characters.")]
    public string Name { get; init; } = string.Empty;

    [Range(1, 12, ErrorMessage = "Grade level must be between 1 and 12.")]
    public byte GradeLevel { get; init; }

    [Required(ErrorMessage = "School year is required.")]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "School year must look like 2026-2027.")]
    public string SchoolYear { get; init; } = string.Empty;

    public int? HomeroomTeacherId { get; init; }
}
