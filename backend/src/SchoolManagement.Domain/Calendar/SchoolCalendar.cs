namespace SchoolManagement.Domain.Calendar;

/// <summary>
/// The Israeli school week: Sunday to Thursday are school days, and a week starts on Sunday.
/// Used by the demo data and by the dashboard (weekly attendance, "exams this week").
/// </summary>
public static class SchoolCalendar
{
    public static bool IsSchoolDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Friday or DayOfWeek.Saturday);

    /// <summary>The Sunday on or before the given date.</summary>
    public static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-(int)date.DayOfWeek);
}
