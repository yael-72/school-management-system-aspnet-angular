namespace SchoolManagement.Application.Events;

/// <summary>
/// The school calendar. Everyone sees school-wide events and the events of the classes she may access.
/// Admins manage every event; a teacher may add events only to her own classes, and edit or delete
/// only the events she created.
/// </summary>
public interface IEventService
{
    /// <summary>Events that overlap the date range (at most 366 days), by start date.</summary>
    Task<IReadOnlyList<EventDto>> GetEventsAsync(EventQuery query, CancellationToken cancellationToken = default);

    Task<EventDto> CreateEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default);

    Task<EventDto> UpdateEventAsync(int id, SaveEventRequest request, CancellationToken cancellationToken = default);

    Task DeleteEventAsync(int id, CancellationToken cancellationToken = default);
}
