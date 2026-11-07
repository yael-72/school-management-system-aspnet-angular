namespace SchoolManagement.Domain.Entities;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}
