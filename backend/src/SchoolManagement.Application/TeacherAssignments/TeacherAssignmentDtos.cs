using System.ComponentModel.DataAnnotations;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.TeacherAssignments;

/// <summary>GET /api/teacher-assignments?classId=3&amp;teacherId=2&amp;subjectId=1&amp;sortBy=className</summary>
public class TeacherAssignmentListQuery : ListQuery
{
    [Range(1, int.MaxValue)]
    public int? ClassId { get; init; }

    [Range(1, int.MaxValue)]
    public int? TeacherId { get; init; }

    [Range(1, int.MaxValue)]
    public int? SubjectId { get; init; }
}

/// <summary>One row of the assignments table.</summary>
/// <remarks>Init properties because the list query sorts on them (see StudentListItemDto).</remarks>
public record TeacherAssignmentDto
{
    public int Id { get; init; }
    public int TeacherId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public bool TeacherIsActive { get; init; }
    public int ClassId { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public byte GradeLevel { get; init; }
    public int SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
}

/// <summary>Body of POST /api/teacher-assignments: "this teacher teaches this subject in this class".</summary>
public record CreateTeacherAssignmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Teacher is required.")]
    public int TeacherId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Class is required.")]
    public int ClassId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Subject is required.")]
    public int SubjectId { get; init; }
}
