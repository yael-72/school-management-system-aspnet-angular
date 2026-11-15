namespace SchoolManagement.Application.Attendance;

/// <summary>
/// The one definition of "attendance rate" used across the app (student summary, reports, dashboard):
/// the share of recorded school days on which the student came to school, on time or late.
/// Excused absences count as recorded days that were missed.
/// </summary>
public static class AttendanceRate
{
    /// <returns>A percentage (0–100) rounded to 1 decimal, or null when no days were recorded.</returns>
    public static decimal? Calculate(int present, int late, int recordedDays) =>
        recordedDays == 0 ? null : Math.Round(100m * (present + late) / recordedDays, 1);
}
