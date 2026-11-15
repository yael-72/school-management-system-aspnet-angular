using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Common.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    /// <summary>Creates a signed JWT for the user. The user's Role must be loaded.</summary>
    AccessToken CreateToken(User user, bool rememberMe);
}
