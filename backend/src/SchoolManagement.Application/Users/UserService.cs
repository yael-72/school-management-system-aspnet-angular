using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Extensions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Users;

/// <summary>
/// Staff list for admins. For now it is read-only and feeds the "homeroom teacher" picker;
/// creating and editing users comes in a later milestone.
/// </summary>
public class UserService(IApplicationDbContext db) : IUserService
{
    public Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            users = users.Where(u => u.Role.Name == query.Role);
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        if (query.SearchTerm is { } search)
        {
            users = users.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
        }

        var projected = users.Select(u => new UserListItemDto(u.Id, u.FullName, u.Email, u.Role.Name, u.Phone, u.IsActive));

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "email" => projected.OrderBy(u => u.Email, query.SortDir),
            "role" => projected.OrderBy(u => u.Role, query.SortDir).ThenBy(u => u.FullName),
            _ => projected.OrderBy(u => u.FullName, query.SortDir)
        };

        return sorted.ThenBy(u => u.Id).ToPagedResultAsync(query, cancellationToken);
    }
}
