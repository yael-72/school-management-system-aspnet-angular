using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Users;

/// <summary>
/// Staff management for admins. Guard rails keep the school manageable: an admin cannot disable,
/// delete or demote herself (so there is always an active admin), and a user with history (grades,
/// attendance, classes) can only be deactivated, not deleted.
/// </summary>
public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);

    Task<UserDto> GetUserAsync(int id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Activates or deactivates a user. A deactivated user cannot log in.</summary>
    Task<UserDto> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Deletes a user who has no history in the school; otherwise asks to deactivate instead.</summary>
    Task DeleteUserAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
}
