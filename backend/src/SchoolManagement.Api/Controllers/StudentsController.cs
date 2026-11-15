using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.Api.Controllers;

/// <summary>
/// Teachers can read the students of the classes they teach in; only admins can enrol,
/// update or remove students.
/// </summary>
[ApiController]
[Route("api/students")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class StudentsController(IStudentService studentService) : ControllerBase
{
    /// <summary>Lists active students, optionally of one class.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<StudentListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StudentListItemDto>>> GetStudents(
        [FromQuery] StudentListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await studentService.GetStudentsAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<StudentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentDto>> GetStudent(int id, CancellationToken cancellationToken)
    {
        return Ok(await studentService.GetStudentAsync(id, cancellationToken));
    }

    /// <summary>The student's average score, attendance rate and most recent scores.</summary>
    [HttpGet("{id:int}/summary")]
    [ProducesResponseType<StudentSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSummaryDto>> GetSummary(int id, CancellationToken cancellationToken)
    {
        return Ok(await studentService.GetSummaryAsync(id, cancellationToken));
    }

    /// <summary>Enrols a new student. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<StudentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StudentDto>> CreateStudent(
        SaveStudentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await studentService.CreateStudentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStudent), new { id = created.Id }, created);
    }

    /// <summary>Updates a student's details or moves them to another class. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<StudentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentDto>> UpdateStudent(
        int id,
        SaveStudentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await studentService.UpdateStudentAsync(id, request, cancellationToken));
    }

    /// <summary>Removes a student (soft delete – grades and attendance are kept). Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudent(int id, CancellationToken cancellationToken)
    {
        await studentService.DeleteStudentAsync(id, cancellationToken);
        return NoContent();
    }
}
