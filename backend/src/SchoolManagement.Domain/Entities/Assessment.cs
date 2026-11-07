using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

/// <summary>
/// An exam, quiz or assignment given to a class in a subject.
/// </summary>
public class Assessment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public AssessmentType Type { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public DateOnly Date { get; set; }
    public decimal MaxScore { get; set; }
    public string? Description { get; set; }
    public int? CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }

    public SchoolClass Class { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public User? CreatedBy { get; set; }
    public ICollection<Score> Scores { get; set; } = new List<Score>();
}
