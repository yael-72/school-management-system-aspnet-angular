using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.TeacherAssignments;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.Api.Controllers;

/// <summary>Who teaches which subject in which class – the rows that give teachers access. Admin only.</summary>
[ApiController]
[Route("api/teacher-assignments")]
[Produces("application/json")]
[Authorize(Roles = RoleNames.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class TeacherAssignmentsController(ITeacherAssignmentService assignmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TeacherAssignmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TeacherAssignmentDto>>> GetAssignments(
        [FromQuery] TeacherAssignmentListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await assignmentService.GetAssignmentsAsync(query, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<TeacherAssignmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherAssignmentDto>> CreateAssignment(
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await assignmentService.CreateAssignmentAsync(request, cancellationToken);
        return Created("/api/teacher-assignments", created);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAssignment(int id, CancellationToken cancellationToken)
    {
        await assignmentService.DeleteAssignmentAsync(id, cancellationToken);
        return NoContent();
    }
}
