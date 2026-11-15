using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Events;

public class EventService(
    IApplicationDbContext db,
    IClassAccessService classAccess,
    ICurrentUser currentUser,
    ILogger<EventService> logger) : IEventService
{
    public const int MaxRangeDays = 366;

    public async Task<IReadOnlyList<EventDto>> GetEventsAsync(EventQuery query, CancellationToken cancellationToken = default)
    {
        var from = query.From!.Value;
        var to = query.To!.Value;

        if (from > to)
        {
            throw new BusinessRuleException("The start date must be on or before the end date.");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
        {
            throw new BusinessRuleException($"The date range can cover at most {MaxRangeDays} days.");
        }

        var events = db.Events.AsNoTracking();

        var accessibleClassIds = classAccess.AccessibleClassIds();
        if (accessibleClassIds is not null)
        {
            events = events.Where(e => e.ClassId == null || accessibleClassIds.Contains(e.ClassId.Value));
        }

        var userId = currentUser.Id;
        var isAdmin = currentUser.IsAdmin;

        // An event overlaps the range when it starts before the range ends and ends after it starts.
        return await events
            .Where(e => e.StartDate <= to && (e.EndDate ?? e.StartDate) >= from)
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.Id)
            .Select(e => new EventDto(
                e.Id,
                e.Title,
                e.StartDate,
                e.EndDate,
                e.ClassId,
                e.Class != null ? e.Class.Name : null,
                isAdmin || e.CreatedByUserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<EventDto> CreateEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var schoolEvent = new SchoolEvent { CreatedByUserId = currentUser.Id };
        Apply(schoolEvent, request);

        db.Events.Add(schoolEvent);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Event {EventId} created by user {UserId}", schoolEvent.Id, currentUser.Id);
        return await GetEventAsync(schoolEvent.Id, cancellationToken);
    }

    public async Task<EventDto> UpdateEventAsync(int id, SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        var schoolEvent = await FindEditableEventAsync(id, cancellationToken);
        await ValidateAsync(request, cancellationToken);

        Apply(schoolEvent, request);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Event {EventId} updated", id);
        return await GetEventAsync(id, cancellationToken);
    }

    public async Task DeleteEventAsync(int id, CancellationToken cancellationToken = default)
    {
        var schoolEvent = await FindEditableEventAsync(id, cancellationToken);

        db.Events.Remove(schoolEvent);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Event {EventId} deleted", id);
    }

    private async Task<EventDto> GetEventAsync(int id, CancellationToken cancellationToken) =>
        await db.Events
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventDto(
                e.Id, e.Title, e.StartDate, e.EndDate, e.ClassId,
                e.Class != null ? e.Class.Name : null,
                true))
            .SingleAsync(cancellationToken);

    /// <summary>404 when it does not exist, 403 when it is not the current user's to change.</summary>
    private async Task<SchoolEvent> FindEditableEventAsync(int id, CancellationToken cancellationToken)
    {
        var schoolEvent = await db.Events.SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Event", id);

        if (!currentUser.IsAdmin && schoolEvent.CreatedByUserId != currentUser.Id)
        {
            throw new ForbiddenException("Only the admin or the teacher who created an event can change it.");
        }

        return schoolEvent;
    }

    private async Task ValidateAsync(SaveEventRequest request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
        {
            throw new BusinessRuleException("The end date must be on or after the start date.");
        }

        if (request.ClassId is not { } classId)
        {
            if (!currentUser.IsAdmin)
            {
                throw new ForbiddenException("Only the admin can add school-wide events.");
            }
            return;
        }

        if (!await db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            throw new BusinessRuleException($"Class with id '{classId}' does not exist.");
        }

        await classAccess.EnsureCanAccessClassAsync(classId, cancellationToken);
    }

    private static void Apply(SchoolEvent schoolEvent, SaveEventRequest request)
    {
        schoolEvent.Title = request.Title.Trim();
        schoolEvent.StartDate = request.StartDate!.Value;
        // A one-day event is stored without an end date.
        schoolEvent.EndDate = request.EndDate == request.StartDate ? null : request.EndDate;
        schoolEvent.ClassId = request.ClassId;
    }
}
