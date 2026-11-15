using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Application.Events;

/// <summary>GET /api/events?from=2026-11-01&amp;to=2026-11-30</summary>
public class EventQuery
{
    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly? From { get; init; }

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly? To { get; init; }
}

/// <param name="EndDate">Last day of a multi-day event; null for a one-day event.</param>
/// <param name="ClassId">Null for a school-wide event.</param>
/// <param name="CanEdit">Whether the current user may edit or delete it.</param>
public record EventDto(
    int Id,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    int? ClassId,
    string? ClassName,
    bool CanEdit);

/// <summary>Body of POST /api/events and PUT /api/events/{id}.</summary>
public record SaveEventRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(100, ErrorMessage = "Title can be at most 100 characters.")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>Null for a school-wide event (admins only).</summary>
    [Range(1, int.MaxValue)]
    public int? ClassId { get; init; }
}
