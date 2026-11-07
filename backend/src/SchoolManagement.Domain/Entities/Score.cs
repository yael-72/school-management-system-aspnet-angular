namespace SchoolManagement.Domain.Entities;

/// <summary>
/// A student's score on one assessment. There is at most one score per (assessment, student).
/// </summary>
public class Score
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public int StudentId { get; set; }
    public decimal Value { get; set; }
    public string? Comment { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
