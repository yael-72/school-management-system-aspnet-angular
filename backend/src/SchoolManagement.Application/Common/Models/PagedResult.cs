namespace SchoolManagement.Application.Common.Models;

/// <summary>
/// One page of a list endpoint. Every list in the API returns this shape:
/// { items, totalCount, page, pageSize }.
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
