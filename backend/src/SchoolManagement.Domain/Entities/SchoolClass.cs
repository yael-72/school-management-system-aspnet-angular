namespace SchoolManagement.Domain.Entities;

/// <summary>
/// A class (homeroom group) such as "ז'1". Named SchoolClass because "Class" is confusing in C# code;
/// the database table is still called Classes.
/// </summary>
public class SchoolClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Grade level 1–12 (the demo school has grades 7–12).</summary>
    public byte GradeLevel { get; set; }

    /// <summary>For example "2026-2027".</summary>
    public string SchoolYear { get; set; } = string.Empty;

    public int? HomeroomTeacherId { get; set; }

    public User? HomeroomTeacher { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<SchoolEvent> Events { get; set; } = new List<SchoolEvent>();
}
