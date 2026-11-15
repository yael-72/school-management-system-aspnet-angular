using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Events;

namespace SchoolManagement.Api.Controllers;

/// <summary>
/// The school calendar. School-wide events are managed by the admin; teachers can add events
/// to their own classes and change the events they created (checked in the service).
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class EventsController(IEventService eventService) : ControllerBase
{
    /// <summary>Events that overlap the date range (school-wide + the user's classes).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EventDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetEvents(
        [FromQuery] EventQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await eventService.GetEventsAsync(query, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<EventDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> CreateEvent(SaveEventRequest request, CancellationToken cancellationToken)
    {
        var created = await eventService.CreateEventAsync(request, cancellationToken);
        return Created("/api/events", created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<EventDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, SaveEventRequest request, CancellationToken cancellationToken)
    {
        return Ok(await eventService.UpdateEventAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken cancellationToken)
    {
        await eventService.DeleteEventAsync(id, cancellationToken);
        return NoContent();
    }
}
