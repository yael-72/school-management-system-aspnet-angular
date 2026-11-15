using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Application.Tasks;

public record TodoTaskDto(int Id, string Title, DateOnly? DueDate, bool IsDone, DateTime CreatedAt);

/// <summary>Body of POST /api/tasks and PUT /api/tasks/{id}.</summary>
public record SaveTodoTaskRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title can be at most 200 characters.")]
    public string Title { get; init; } = string.Empty;

    public DateOnly? DueDate { get; init; }

    public bool IsDone { get; init; }
}
