using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Application.Auth;

public record LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not valid.")]
    [MaxLength(150)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    /// <summary>"Keep me logged in": issues a longer-lived token.</summary>
    public bool RememberMe { get; init; }
}

public record CurrentUserDto(int Id, string FullName, string Email, string Role);

public record LoginResponse(string AccessToken, DateTime ExpiresAt, CurrentUserDto User);
