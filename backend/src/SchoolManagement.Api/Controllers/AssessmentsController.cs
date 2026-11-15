using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Assessments;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Api.Controllers;

/// <summary>
/// Exams, quizzes and assignments and their scores. Everyone reads the assessments of the classes
/// she may access; editing and grading need permission for the subject in that class
/// (checked in the service, because it depends on the data, not only on the role).
/// </summary>
[ApiController]
[Route("api/assessments")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class AssessmentsController(IAssessmentService assessmentService) : ControllerBase
{
    /// <summary>Lists assessments, optionally filtered by class, subject, type and date range.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AssessmentListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AssessmentListItemDto>>> GetAssessments(
        [FromQuery] AssessmentListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await assessmentService.GetAssessmentsAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<AssessmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentDto>> GetAssessment(int id, CancellationToken cancellationToken)
    {
        return Ok(await assessmentService.GetAssessmentAsync(id, cancellationToken));
    }

    /// <summary>Creates an assessment for a subject in a class.</summary>
    [HttpPost]
    [ProducesResponseType<AssessmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssessmentDto>> CreateAssessment(
        SaveAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await assessmentService.CreateAssessmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAssessment), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AssessmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentDto>> UpdateAssessment(
        int id,
        SaveAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await assessmentService.UpdateAssessmentAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes the assessment and all its scores.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAssessment(int id, CancellationToken cancellationToken)
    {
        await assessmentService.DeleteAssessmentAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>The grade sheet: every active student of the class with their score.</summary>
    [HttpGet("{id:int}/scores")]
    [ProducesResponseType<AssessmentScoresDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentScoresDto>> GetScores(int id, CancellationToken cancellationToken)
    {
        return Ok(await assessmentService.GetScoresAsync(id, cancellationToken));
    }

    /// <summary>
    /// Saves the scores of the class (creates or updates one score per student; a row with no score
    /// and no comment clears it). Scores must be between 0 and the assessment's max score.
    /// </summary>
    [HttpPut("{id:int}/scores")]
    [ProducesResponseType<AssessmentScoresDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentScoresDto>> SaveScores(
        int id,
        SaveScoresRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await assessmentService.SaveScoresAsync(id, request, cancellationToken));
    }
}
