namespace SchoolManagement.Application.Attendance;

public interface IAttendanceService
{
    /// <summary>The register of a class on one day: all active students and their status, if recorded.</summary>
    Task<ClassAttendanceDto> GetClassAttendanceAsync(ClassAttendanceQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the statuses of a class on one day. Existing records of that day are updated and
    /// missing ones are created (upsert), so saving the same day twice never creates duplicates.
    /// </summary>
    Task<ClassAttendanceDto> SaveClassAttendanceAsync(SaveAttendanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Per-student totals of each status over a date range.</summary>
    Task<AttendanceReportDto> GetReportAsync(AttendanceReportQuery query, CancellationToken cancellationToken = default);
}
