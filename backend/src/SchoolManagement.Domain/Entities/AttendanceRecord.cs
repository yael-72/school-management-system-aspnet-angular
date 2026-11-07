using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Attendance of one student on one day. There is at most one record per (student, date).
/// </summary>
public class AttendanceRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
    public int RecordedByUserId { get; set; }

    public Student Student { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
