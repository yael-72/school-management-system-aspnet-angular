using System.ComponentModel.DataAnnotations;
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

    /// <summary>How many (class, subject) pairs the user teaches.</summary>
    public int AssignmentCount { get; init; }
}

/// <summary>GET /api/users/{id} and the result of create / update.</summary>
public record UserDto(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    int RoleId,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>Fields shared by create and update.</summary>
public abstract record SaveUserRequestBase
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100, ErrorMessage = "Full name can be at most 100 characters.")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not valid.")]
    [MaxLength(150, ErrorMessage = "Email can be at most 150 characters.")]
    public string Email { get; init; } = string.Empty;

    [RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "Phone number is not valid.")]
    public string? Phone { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Role is required.")]
    public int RoleId { get; init; }
}

/// <summary>Body of POST /api/users. The admin sets a first password and gives it to the new user.</summary>
public record CreateUserRequest : SaveUserRequestBase
{
    // At least 8 characters with a letter and a digit – the same rule the Angular form shows.
    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8–100 characters and contain a letter and a digit.")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>Body of PUT /api/users/{id}. The password is not changed here.</summary>
public record UpdateUserRequest : SaveUserRequestBase;

/// <summary>Body of PATCH /api/users/{id}/status.</summary>
public record UpdateUserStatusRequest
{
    [Required(ErrorMessage = "IsActive is required.")]
    public bool? IsActive { get; init; }
}

public record RoleDto(int Id, string Name);
