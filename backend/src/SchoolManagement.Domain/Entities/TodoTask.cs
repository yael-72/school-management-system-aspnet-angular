namespace SchoolManagement.Domain.Entities;

/// <summary>
/// A personal to-do item shown on the user's dashboard.
/// </summary>
public class TodoTask
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
