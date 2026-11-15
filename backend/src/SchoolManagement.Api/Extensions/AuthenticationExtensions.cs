using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SchoolManagement.Infrastructure.Identity;

namespace SchoolManagement.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep the short JWT claim names ("sub", "role") instead of mapping them to long URIs.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    NameClaimType = AuthClaimTypes.Name,
                    RoleClaimType = AuthClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        // Secure by default: every endpoint requires a logged-in user unless it says [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
