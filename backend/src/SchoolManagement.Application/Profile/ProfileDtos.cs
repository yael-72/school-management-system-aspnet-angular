using System.ComponentModel.DataAnnotations;
using SchoolManagement.Application.Common.Security;

namespace SchoolManagement.Application.Profile;

/// <summary>GET /api/users/me – the logged-in user's profile page.</summary>
/// <param name="ClassCount">Classes the user may access (all classes for an admin).</param>
/// <param name="StudentCount">Active students in those classes.</param>
/// <param name="AssessmentCount">Assessments the user created.</param>
public record ProfileDto(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string Role,
    DateTime CreatedAt,
    int ClassCount,
    int StudentCount,
    int AssessmentCount);

/// <summary>
/// Body of PUT /api/users/me. The email is the login name and the role is set by the admin,
/// so a user can change only her name and phone here.
/// </summary>
public record UpdateProfileRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100, ErrorMessage = "Full name can be at most 100 characters.")]
    public string FullName { get; init; } = string.Empty;

    [RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "Phone number is not valid.")]
    public string? Phone { get; init; }
}

/// <summary>GET and PUT /api/users/me/notification-settings.</summary>
public record NotificationSettingsDto
{
    public bool RemindBeforeExam { get; init; }
    public bool AlertOnLowAttendance { get; init; }
    public bool WeeklySummaryEmail { get; init; }
}

/// <summary>Body of PUT /api/users/me/password. The current password proves it is really the user.</summary>
public record ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required.")]
    [MaxLength(100)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.ErrorMessage)]
    public string NewPassword { get; init; } = string.Empty;
}
