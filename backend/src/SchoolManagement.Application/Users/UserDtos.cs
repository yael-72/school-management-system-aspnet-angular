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

public record UserListItemDto(int Id, string FullName, string Email, string Role, string? Phone, bool IsActive);
