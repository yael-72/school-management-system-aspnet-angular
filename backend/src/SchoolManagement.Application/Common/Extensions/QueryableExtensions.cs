using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Common.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Runs two queries: COUNT(*) for the total, and the requested page (OFFSET/FETCH in SQL).
    /// The query must already be sorted, otherwise the pages are not stable.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        ListQuery listQuery,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((listQuery.Page - 1) * listQuery.PageSize)
            .Take(listQuery.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, listQuery.Page, listQuery.PageSize);
    }

    /// <summary>OrderBy or OrderByDescending, chosen by the sort direction from the query string.</summary>
    public static IOrderedQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction) =>
        direction == SortDirection.Desc ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    /// <summary>ThenBy or ThenByDescending, chosen by the sort direction from the query string.</summary>
    public static IOrderedQueryable<T> ThenBy<T, TKey>(
        this IOrderedQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction) =>
        direction == SortDirection.Desc ? query.ThenByDescending(keySelector) : query.ThenBy(keySelector);
}
