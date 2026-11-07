namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Per-user notification toggles (one row per user, the user id is also the primary key).
/// </summary>
public class NotificationSettings
{
    public int UserId { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public bool AbsenceAlerts { get; set; } = true;
    public bool GradeAlerts { get; set; } = true;
    public bool EventReminders { get; set; } = true;
    public bool WeeklySummary { get; set; }

    public User User { get; set; } = null!;
}
