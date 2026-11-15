using System.ComponentModel.DataAnnotations;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Attendance;

/// <summary>GET /api/attendance?classId=3&amp;date=2026-11-10</summary>
public class ClassAttendanceQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "Class is required.")]
    public int ClassId { get; init; }

    // Nullable + [Required] so a missing date is a validation error instead of silently becoming 0001-01-01.
    [Required(ErrorMessage = "Date is required.")]
    public DateOnly? Date { get; init; }
}

/// <summary>One row of the attendance register. Status is null when nothing was recorded yet.</summary>
public record AttendanceEntryDto(
    int StudentId,
    string FirstName,
    string LastName,
    AttendanceStatus? Status,
    string? Note);

/// <summary>
/// The attendance register of one class on one day: every active student, with the status
/// recorded for that day (if any). Not paged – a class has a few dozen students at most.
/// </summary>
public record ClassAttendanceDto(
    int ClassId,
    string ClassName,
    DateOnly Date,
    IReadOnlyList<AttendanceEntryDto> Students);

/// <summary>Body of PUT /api/attendance: the statuses of a class on one day.</summary>
public record SaveAttendanceRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Class is required.")]
    public int ClassId { get; init; }

    [Required(ErrorMessage = "Date is required.")]
    public DateOnly? Date { get; init; }

    [Required(ErrorMessage = "At least one attendance record is required.")]
    [MinLength(1, ErrorMessage = "At least one attendance record is required.")]
    public IReadOnlyList<SaveAttendanceEntry> Records { get; init; } = [];
}

public record SaveAttendanceEntry
{
    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; init; }

    // EnumDataType rejects numbers that are not a defined status (for example 42).
    [Required(ErrorMessage = "Status is required.")]
    [EnumDataType(typeof(AttendanceStatus), ErrorMessage = "Status is not valid.")]
    public AttendanceStatus? Status { get; init; }

    [MaxLength(200, ErrorMessage = "Note can be at most 200 characters.")]
    public string? Note { get; init; }
}

/// <summary>GET /api/attendance/report?classId=3&amp;from=2026-10-01&amp;to=2026-10-31</summary>
public class AttendanceReportQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "Class is required.")]
    public int ClassId { get; init; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly? From { get; init; }

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly? To { get; init; }
}

/// <summary>How many days of each status one student had in the report's date range.</summary>
public record StudentAttendanceSummaryDto(
    int StudentId,
    string FirstName,
    string LastName,
    int Present,
    int Absent,
    int Late,
    int Excused,
    decimal? AttendanceRate);

/// <param name="AttendanceRate">The rate of the whole class over the range (0–100), null when nothing was recorded.</param>
public record AttendanceReportDto(
    int ClassId,
    string ClassName,
    DateOnly From,
    DateOnly To,
    decimal? AttendanceRate,
    IReadOnlyList<StudentAttendanceSummaryDto> Students);
