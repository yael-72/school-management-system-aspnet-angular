using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.Api.Controllers;

/// <summary>Every user reads the subjects (filters, forms); only admins change them.</summary>
[ApiController]
[Route("api/subjects")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class SubjectsController(ISubjectService subjectService) : ControllerBase
{
    /// <summary>All subjects, alphabetically.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SubjectDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubjectDto>>> GetSubjects(CancellationToken cancellationToken)
    {
        return Ok(await subjectService.GetSubjectsAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<SubjectDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SubjectDto>> CreateSubject(SaveSubjectRequest request, CancellationToken cancellationToken)
    {
        var created = await subjectService.CreateSubjectAsync(request, cancellationToken);
        return Created("/api/subjects", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<SubjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectDto>> UpdateSubject(int id, SaveSubjectRequest request, CancellationToken cancellationToken)
    {
        return Ok(await subjectService.UpdateSubjectAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes a subject that is not taught and has no assessments.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubject(int id, CancellationToken cancellationToken)
    {
        await subjectService.DeleteSubjectAsync(id, cancellationToken);
        return NoContent();
    }
}
