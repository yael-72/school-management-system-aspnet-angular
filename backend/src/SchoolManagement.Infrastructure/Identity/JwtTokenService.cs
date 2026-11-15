using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Identity;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateToken(User user, bool rememberMe)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = rememberMe
            ? now.AddDays(_options.RememberMeDays)
            : now.AddMinutes(_options.AccessTokenMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(AuthClaimTypes.Role, user.Role.Name)
            ])
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
