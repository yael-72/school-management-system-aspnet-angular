namespace SchoolManagement.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserDto> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
