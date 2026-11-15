using System.ComponentModel.DataAnnotations;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Students;

/// <summary>GET /api/students?classId=3&amp;search=...&amp;sortBy=lastName</summary>
public class StudentListQuery : ListQuery
{
    [Range(1, int.MaxValue)]
    public int? ClassId { get; init; }
}

/// <summary>One row of the students table.</summary>
/// <remarks>
/// Uses init properties (not a positional record) because list queries sort on these properties:
/// EF Core can translate ORDER BY on a DTO built with an object initializer, but not on one built
/// through a constructor.
/// </remarks>
public record StudentListItemDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public DateOnly? BirthDate { get; init; }
    public string? ParentPhone { get; init; }

    /// <summary>Average of the student's graded scores, as a percentage (0–100).</summary>
    public decimal? AverageScore { get; init; }
}

public record StudentDto(
    int Id,
    string FirstName,
    string LastName,
    int ClassId,
    string ClassName,
    DateOnly? BirthDate,
    string? ParentPhone,
    string? Address);

public record RecentScoreDto(
    int AssessmentId,
    string AssessmentTitle,
    string SubjectName,
    DateOnly Date,
    decimal Score,
    decimal MaxScore,
    decimal Percent);

/// <summary>GET /api/students/{id}/summary – the student's details page.</summary>
/// <param name="AverageScore">Average of all graded scores, as a percentage (0–100).</param>
/// <param name="AttendanceRate">Share of recorded school days the student was present or late (0–100).</param>
public record StudentSummaryDto(
    StudentDto Student,
    decimal? AverageScore,
    decimal? AttendanceRate,
    int AbsenceCount,
    int LateCount,
    IReadOnlyList<RecentScoreDto> RecentScores);

/// <summary>Body of POST /api/students and PUT /api/students/{id}.</summary>
public record SaveStudentRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(50, ErrorMessage = "First name can be at most 50 characters.")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(50, ErrorMessage = "Last name can be at most 50 characters.")]
    public string LastName { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Class is required.")]
    public int ClassId { get; init; }

    public DateOnly? BirthDate { get; init; }

    [RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "Phone number is not valid.")]
    public string? ParentPhone { get; init; }

    [MaxLength(200, ErrorMessage = "Address can be at most 200 characters.")]
    public string? Address { get; init; }
}
