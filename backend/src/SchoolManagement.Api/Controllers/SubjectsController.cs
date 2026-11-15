using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Subjects;

namespace SchoolManagement.Api.Controllers;

[ApiController]
[Route("api/subjects")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class SubjectsController(ISubjectService subjectService) : ControllerBase
{
    /// <summary>All subjects, alphabetically (for filters and forms).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SubjectDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubjectDto>>> GetSubjects(CancellationToken cancellationToken)
    {
        return Ok(await subjectService.GetSubjectsAsync(cancellationToken));
    }
}
