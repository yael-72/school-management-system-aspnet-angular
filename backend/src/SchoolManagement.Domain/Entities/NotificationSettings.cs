namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Per-user notification toggles (one row per user, the user id is also the primary key).
/// </summary>
public class NotificationSettings
{
    public int UserId { get; set; }
    public bool RemindBeforeExam { get; set; } = true;
    public bool AlertOnLowAttendance { get; set; } = true;
    public bool WeeklySummaryEmail { get; set; }

    public User User { get; set; } = null!;
}
