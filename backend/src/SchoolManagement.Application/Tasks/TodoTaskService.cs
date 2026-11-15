using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Tasks;

public class TodoTaskService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ITodoTaskService
{
    public async Task<IReadOnlyList<TodoTaskDto>> GetTasksAsync(CancellationToken cancellationToken = default) =>
        await db.TodoTasks
            .AsNoTracking()
            .Where(t => t.UserId == currentUser.Id)
            .OrderBy(t => t.IsDone)
            .ThenBy(t => t.DueDate == null) // tasks without a due date after the dated ones
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Select(t => new TodoTaskDto(t.Id, t.Title, t.DueDate, t.IsDone, t.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<TodoTaskDto> CreateTaskAsync(SaveTodoTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = new TodoTask
        {
            UserId = currentUser.Id,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        Apply(task, request);

        db.TodoTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(task);
    }

    public async Task<TodoTaskDto> UpdateTaskAsync(int id, SaveTodoTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await FindOwnTaskAsync(id, cancellationToken);
        Apply(task, request);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(task);
    }

    public async Task<TodoTaskDto> ToggleTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await FindOwnTaskAsync(id, cancellationToken);
        task.IsDone = !task.IsDone;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(task);
    }

    public async Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await FindOwnTaskAsync(id, cancellationToken);
        db.TodoTasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TodoTask> FindOwnTaskAsync(int id, CancellationToken cancellationToken) =>
        await db.TodoTasks.SingleOrDefaultAsync(t => t.Id == id && t.UserId == currentUser.Id, cancellationToken)
        ?? throw new NotFoundException("Task", id);

    private static void Apply(TodoTask task, SaveTodoTaskRequest request)
    {
        task.Title = request.Title.Trim();
        task.DueDate = request.DueDate;
        task.IsDone = request.IsDone;
    }

    private static TodoTaskDto ToDto(TodoTask t) => new(t.Id, t.Title, t.DueDate, t.IsDone, t.CreatedAt);
}
