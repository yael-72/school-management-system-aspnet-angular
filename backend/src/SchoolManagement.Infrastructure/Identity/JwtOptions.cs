using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Infrastructure.Identity;

/// <summary>
/// JWT settings, bound from the "Jwt" section of appsettings. The signing key is a secret and is
/// never stored in the repository: it comes from user-secrets or an environment variable (Jwt__SigningKey).
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>At least 32 characters (256 bits) for HMAC-SHA256.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 480;

    /// <summary>Token lifetime when the user ticks "keep me logged in".</summary>
    [Range(1, 30)]
    public int RememberMeDays { get; set; } = 7;
}
