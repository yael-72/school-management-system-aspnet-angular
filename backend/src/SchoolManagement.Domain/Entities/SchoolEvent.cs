namespace SchoolManagement.Domain.Entities;

/// <summary>
/// A calendar event. When ClassId is null the event is school-wide.
/// Named SchoolEvent to avoid clashing with the C# "event" keyword; the table is called Events.
/// </summary>
public class SchoolEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? ClassId { get; set; }
    public int CreatedByUserId { get; set; }

    public SchoolClass? Class { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
