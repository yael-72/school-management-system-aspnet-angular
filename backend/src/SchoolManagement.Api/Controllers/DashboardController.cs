using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Dashboard;

namespace SchoolManagement.Api.Controllers;

/// <summary>
/// The numbers behind the dashboard. Admins get the whole school; teachers get the classes
/// they teach in or are homeroom teacher of (applied in the service).
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>KPI cards: students, classes, today's attendance, average score, exams this week, frequent absences.</summary>
    [HttpGet("stats")]
    [ProducesResponseType<DashboardStatsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetStatsAsync(cancellationToken));
    }

    /// <summary>Average score per month (line chart).</summary>
    [HttpGet("grades-trend")]
    [ProducesResponseType<IReadOnlyList<MonthlyAverageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MonthlyAverageDto>>> GetGradesTrend(
        [FromQuery] GradesTrendQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetGradesTrendAsync(query.Months, cancellationToken));
    }

    /// <summary>Attendance per school day of the last week (bar chart).</summary>
    [HttpGet("attendance-weekly")]
    [ProducesResponseType<IReadOnlyList<DailyAttendanceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyAttendanceDto>>> GetWeeklyAttendance(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetWeeklyAttendanceAsync(cancellationToken));
    }

    /// <summary>How many scores fall in each grade band (pie chart).</summary>
    [HttpGet("grade-distribution")]
    [ProducesResponseType<GradeDistributionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GradeDistributionDto>> GetGradeDistribution(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetGradeDistributionAsync(cancellationToken));
    }

    [HttpGet("top-students")]
    [ProducesResponseType<IReadOnlyList<TopStudentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TopStudentDto>>> GetTopStudents(
        [FromQuery] TopListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetTopStudentsAsync(query.Count, cancellationToken));
    }

    /// <summary>The latest assessments created and graded.</summary>
    [HttpGet("recent-activity")]
    [ProducesResponseType<IReadOnlyList<RecentActivityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RecentActivityDto>>> GetRecentActivity(
        [FromQuery] TopListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetRecentActivityAsync(query.Count, cancellationToken));
    }
}
