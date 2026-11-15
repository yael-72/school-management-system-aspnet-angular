namespace SchoolManagement.Application.Tasks;

/// <summary>
/// The current user's personal to-do list. A task of another user is reported as not found (404),
/// not forbidden, so the API does not reveal which task ids exist.
/// </summary>
public interface ITodoTaskService
{
    /// <summary>Open tasks first (by due date), then done ones. Not paged: a to-do list is short.</summary>
    Task<IReadOnlyList<TodoTaskDto>> GetTasksAsync(CancellationToken cancellationToken = default);

    Task<TodoTaskDto> CreateTaskAsync(SaveTodoTaskRequest request, CancellationToken cancellationToken = default);

    Task<TodoTaskDto> UpdateTaskAsync(int id, SaveTodoTaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marks the task done, or open again.</summary>
    Task<TodoTaskDto> ToggleTaskAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default);
}
