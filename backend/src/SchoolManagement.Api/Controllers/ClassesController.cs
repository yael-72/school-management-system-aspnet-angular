using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.Api.Controllers;

/// <summary>
/// Teachers can read the classes they teach in; only admins can create, update or delete classes.
/// </summary>
[ApiController]
[Route("api/classes")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class ClassesController(IClassService classService, IStudentService studentService) : ControllerBase
{
    /// <summary>Lists classes (for a teacher: only the classes she teaches in or is homeroom teacher of).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ClassSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ClassSummaryDto>>> GetClasses(
        [FromQuery] ClassListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.GetClassesAsync(query, cancellationToken));
    }

    /// <summary>Returns one class with its subjects and teachers.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ClassDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassDetailsDto>> GetClass(int id, CancellationToken cancellationToken)
    {
        return Ok(await classService.GetClassAsync(id, cancellationToken));
    }

    /// <summary>Lists the active students of a class.</summary>
    [HttpGet("{id:int}/students")]
    [ProducesResponseType<PagedResult<StudentListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<StudentListItemDto>>> GetClassStudents(
        int id,
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken)
    {
        // Makes sure the class exists (404) and that the user may see it (403).
        await classService.GetClassAsync(id, cancellationToken);

        var studentQuery = new StudentListQuery
        {
            ClassId = id,
            Page = query.Page,
            PageSize = query.PageSize,
            Search = query.Search,
            SortBy = query.SortBy,
            SortDir = query.SortDir
        };
        return Ok(await studentService.GetStudentsAsync(studentQuery, cancellationToken));
    }

    /// <summary>Creates a class. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<ClassDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClassDetailsDto>> CreateClass(
        SaveClassRequest request,
        CancellationToken cancellationToken)
    {
        var created = await classService.CreateClassAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetClass), new { id = created.Id }, created);
    }

    /// <summary>Updates a class. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<ClassDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassDetailsDto>> UpdateClass(
        int id,
        SaveClassRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await classService.UpdateClassAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes a class. Rejected while the class has students or assessments. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClass(int id, CancellationToken cancellationToken)
    {
        await classService.DeleteClassAsync(id, cancellationToken);
        return NoContent();
    }
}
