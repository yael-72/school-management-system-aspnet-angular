namespace SchoolManagement.Domain.Entities;

/// <summary>
/// A calendar event. When ClassId is null the event is school-wide.
/// Named SchoolEvent to avoid clashing with the C# "event" keyword; the table is called Events.
/// </summary>
public class SchoolEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public int? ClassId { get; set; }
    public int? CreatedById { get; set; }

    public SchoolClass? Class { get; set; }
    public User? CreatedBy { get; set; }
}
