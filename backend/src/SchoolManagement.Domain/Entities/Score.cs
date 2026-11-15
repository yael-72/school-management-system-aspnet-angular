namespace SchoolManagement.Domain.Entities;

/// <summary>
/// A student's score on one assessment. There is at most one score per (assessment, student).
/// </summary>
public class Score
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public int StudentId { get; set; }

    /// <summary>
    /// Null means "not graded yet" (for example the student missed the exam).
    /// Stored in the "Score" column; the C# property cannot have the same name as its class.
    /// </summary>
    public decimal? Value { get; set; }

    public string? Comment { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
