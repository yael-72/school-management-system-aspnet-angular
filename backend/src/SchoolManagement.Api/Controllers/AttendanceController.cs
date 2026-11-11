using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Attendance;

namespace SchoolManagement.Api.Controllers;

/// <summary>
/// Admins and teachers record attendance; a teacher only for the classes she teaches in
/// or is homeroom teacher of (checked in the service).
/// </summary>
[ApiController]
[Route("api/attendance")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    /// <summary>The attendance register of a class on one day (all active students).</summary>
    [HttpGet]
    [ProducesResponseType<ClassAttendanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClassAttendanceDto>> GetClassAttendance(
        [FromQuery] ClassAttendanceQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await attendanceService.GetClassAttendanceAsync(query, cancellationToken));
    }

    /// <summary>
    /// Saves the attendance of a class on one day. Creates or updates one record per student
    /// (never duplicates). Future dates are rejected.
    /// </summary>
    [HttpPut]
    [ProducesResponseType<ClassAttendanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClassAttendanceDto>> SaveClassAttendance(
        SaveAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await attendanceService.SaveClassAttendanceAsync(request, cancellationToken));
    }

    /// <summary>Per-student totals of present, absent, late and excused days over a date range.</summary>
    [HttpGet("report")]
    [ProducesResponseType<AttendanceReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttendanceReportDto>> GetReport(
        [FromQuery] AttendanceReportQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await attendanceService.GetReportAsync(query, cancellationToken));
    }
}
