using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Application.Auth;

public class AuthService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ILogger<AuthService> logger) : IAuthService
{
    // The same message for "no such email" and "wrong password", so the API does not reveal
    // which emails exist in the system.
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Emails are stored in lower case, so the comparison is case-insensitive in every database.
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            logger.LogWarning("Failed login attempt for {Email}", email);
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login attempt for inactive user {UserId}", user.Id);
            throw new AuthenticationFailedException("This account is disabled.");
        }

        var token = tokenService.CreateToken(user, request.RememberMe);
        logger.LogInformation("User {UserId} logged in", user.Id);

        return new LoginResponse(
            token.Token,
            token.ExpiresAt,
            new CurrentUserDto(user.Id, user.FullName, user.Email, user.Role.Name));
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new CurrentUserDto(u.Id, u.FullName, u.Email, u.Role.Name))
            .SingleOrDefaultAsync(cancellationToken);

        // The token is valid but the user was deleted or disabled after it was issued.
        return user ?? throw new AuthenticationFailedException("The user is no longer active.");
    }
}
