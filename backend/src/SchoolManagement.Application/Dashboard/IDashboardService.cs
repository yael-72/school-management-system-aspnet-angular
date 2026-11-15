namespace SchoolManagement.Application.Dashboard;

/// <summary>
/// Read-only numbers for the dashboard. Everything is scoped by role: an admin sees the whole
/// school, a teacher only the classes she may access. Inactive students are always left out.
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Monthly average score for the last <paramref name="months"/> months, oldest first, including empty months.</summary>
    Task<IReadOnlyList<MonthlyAverageDto>> GetGradesTrendAsync(int months, CancellationToken cancellationToken = default);

    /// <summary>Attendance counts for each school day of the last 7 days, oldest first.</summary>
    Task<IReadOnlyList<DailyAttendanceDto>> GetWeeklyAttendanceAsync(CancellationToken cancellationToken = default);

    Task<GradeDistributionDto> GetGradeDistributionAsync(CancellationToken cancellationToken = default);

    /// <summary>The students with the highest average score.</summary>
    Task<IReadOnlyList<TopStudentDto>> GetTopStudentsAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>The latest assessments created and graded, newest first.</summary>
    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(int count, CancellationToken cancellationToken = default);
}
