using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);
}
