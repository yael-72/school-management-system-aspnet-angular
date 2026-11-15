using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Tasks;

namespace SchoolManagement.Api.Controllers;

/// <summary>The logged-in user's personal to-do list (dashboard widget).</summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class TasksController(ITodoTaskService taskService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TodoTaskDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TodoTaskDto>>> GetTasks(CancellationToken cancellationToken)
    {
        return Ok(await taskService.GetTasksAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<TodoTaskDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoTaskDto>> CreateTask(SaveTodoTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await taskService.CreateTaskAsync(request, cancellationToken);
        // There is no GET /api/tasks/{id}, so the Location header points to the list.
        return Created("/api/tasks", created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<TodoTaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoTaskDto>> UpdateTask(int id, SaveTodoTaskRequest request, CancellationToken cancellationToken)
    {
        return Ok(await taskService.UpdateTaskAsync(id, request, cancellationToken));
    }

    /// <summary>Marks the task done, or open again.</summary>
    [HttpPatch("{id:int}/toggle")]
    [ProducesResponseType<TodoTaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoTaskDto>> ToggleTask(int id, CancellationToken cancellationToken)
    {
        return Ok(await taskService.ToggleTaskAsync(id, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(int id, CancellationToken cancellationToken)
    {
        await taskService.DeleteTaskAsync(id, cancellationToken);
        return NoContent();
    }
}
