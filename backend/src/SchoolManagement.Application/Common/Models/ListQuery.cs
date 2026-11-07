using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Application.Common.Models;

public enum SortDirection
{
    Asc,
    Desc
}

/// <summary>
/// The query-string parameters shared by every list endpoint
/// (?page=1&amp;pageSize=20&amp;search=...&amp;sortBy=name&amp;sortDir=asc).
/// Feature-specific queries inherit from it and add their own filters.
/// </summary>
public class ListQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 20;

    [MaxLength(100)]
    public string? Search { get; init; }

    /// <summary>Field name to sort by. Unknown names fall back to the default order of the list.</summary>
    [MaxLength(50)]
    public string? SortBy { get; init; }

    public SortDirection SortDir { get; init; } = SortDirection.Asc;

    /// <summary>The search text without surrounding spaces, or null when there is nothing to search for.</summary>
    public string? SearchTerm => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
