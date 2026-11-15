using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Application.Dashboard;

/// <summary>A student who was absent often lately (dashboard warning list).</summary>
public record FrequentAbsenceDto(int StudentId, string FirstName, string LastName, string ClassName, int AbsenceCount);

/// <summary>GET /api/dashboard/stats – the KPI cards at the top of the dashboard.</summary>
/// <param name="TodayAttendanceRate">Share of today's recorded students who came (0–100); null when nothing was recorded yet.</param>
/// <param name="AverageScore">Average of all graded scores, as a percentage (0–100).</param>
/// <param name="ExamsThisWeek">Assessments dated in the current school week (Sunday to Saturday).</param>
/// <param name="FrequentAbsences">Students with many absences in the last <see cref="DashboardService.AbsenceWindowDays"/> days, most absences first.</param>
public record DashboardStatsDto(
    int StudentCount,
    int ClassCount,
    decimal? TodayAttendanceRate,
    decimal? AverageScore,
    int ExamsThisWeek,
    IReadOnlyList<FrequentAbsenceDto> FrequentAbsences);

/// <summary>One point of the grades trend line: the average of a month (null when nothing was graded).</summary>
public record MonthlyAverageDto(int Year, int Month, decimal? AverageScore);

/// <summary>One bar of the weekly attendance chart.</summary>
public record DailyAttendanceDto(DateOnly Date, int Present, int Late, int Absent, int Excused, decimal? AttendanceRate);

/// <summary>
/// How many scores fall in each grade band (the same bands the app uses to color scores):
/// excellent ≥ 90%, good ≥ 75%, fair ≥ 55%, low below that.
/// </summary>
public record GradeDistributionDto(int Excellent, int Good, int Fair, int Low);

public record TopStudentDto(int StudentId, string FirstName, string LastName, string ClassName, decimal AverageScore);

public enum ActivityKind
{
    AssessmentCreated,
    ScoresEntered
}

/// <param name="UserName">Who did it, when known (scores do not record who entered them).</param>
public record RecentActivityDto(
    ActivityKind Kind,
    int AssessmentId,
    string Title,
    string ClassName,
    string SubjectName,
    DateTime OccurredAt,
    string? UserName);

/// <summary>GET /api/dashboard/grades-trend?months=6</summary>
public class GradesTrendQuery
{
    [Range(1, 12, ErrorMessage = "Months must be between 1 and 12.")]
    public int Months { get; init; } = 6;
}

/// <summary>GET /api/dashboard/top-students?count=5 and /recent-activity?count=8</summary>
public class TopListQuery
{
    [Range(1, 20, ErrorMessage = "Count must be between 1 and 20.")]
    public int Count { get; init; } = 5;
}
