namespace SchoolManagement.Domain.Entities;

public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? ParentPhone { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// Soft delete flag. Students are never physically deleted so their grade and attendance history is kept.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public SchoolClass Class { get; set; } = null!;
    public ICollection<Score> Scores { get; set; } = new List<Score>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public string FullName => $"{FirstName} {LastName}";
}
