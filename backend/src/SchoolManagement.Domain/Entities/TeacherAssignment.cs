namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Says that a teacher teaches a subject in a class. These rows (not the user's role)
/// decide which classes and subjects a teacher may see and edit.
/// </summary>
public class TeacherAssignment
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }

    public User Teacher { get; set; } = null!;
    public SchoolClass Class { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
