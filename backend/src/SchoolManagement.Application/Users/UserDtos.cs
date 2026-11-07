using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Users;

/// <summary>GET /api/users?role=Teacher&amp;search=...</summary>
public class UserListQuery : ListQuery
{
    /// <summary>Only users with this role name (Admin / Teacher).</summary>
    public string? Role { get; init; }

    /// <summary>Only active (true) or only disabled (false) users. Both when not set.</summary>
    public bool? IsActive { get; init; }
}

/// <summary>One staff member in the users list.</summary>
/// <remarks>
/// Uses init properties (not a positional record) because list queries sort on these properties:
/// EF Core can translate ORDER BY on a DTO built with an object initializer, but not on one built
/// through a constructor.
/// </remarks>
public record UserListItemDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
