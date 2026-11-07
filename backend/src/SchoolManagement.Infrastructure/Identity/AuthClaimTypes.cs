using Microsoft.IdentityModel.JsonWebTokens;

namespace SchoolManagement.Infrastructure.Identity;

/// <summary>
/// Claim names used in our tokens. Short JWT names ("sub", "role") are used as-is, without
/// mapping them to the long Microsoft claim URIs.
/// </summary>
public static class AuthClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string Role = "role";
}
