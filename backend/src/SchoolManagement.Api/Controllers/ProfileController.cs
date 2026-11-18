using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Profile;

namespace SchoolManagement.Api.Controllers;

/// <summary>
/// The logged-in user's own profile. A separate controller from UsersController because that one is
/// admin-only, while every user may read and edit her own profile.
/// </summary>
[ApiController]
[Route("api/users/me")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class ProfileController(IProfileService profileService) : ControllerBase
{
    /// <summary>The user's details and a few numbers about her classes.</summary>
    [HttpGet]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        return Ok(await profileService.GetProfileAsync(cancellationToken));
    }

    /// <summary>Updates the user's name and phone.</summary>
    [HttpPut]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        return Ok(await profileService.UpdateProfileAsync(request, cancellationToken));
    }

    /// <summary>Changes the user's own password. A wrong current password is a 400, not a 401.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await profileService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("notification-settings")]
    [ProducesResponseType<NotificationSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> GetNotificationSettings(CancellationToken cancellationToken)
    {
        return Ok(await profileService.GetNotificationSettingsAsync(cancellationToken));
    }

    [HttpPut("notification-settings")]
    [ProducesResponseType<NotificationSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotificationSettingsDto>> UpdateNotificationSettings(
        NotificationSettingsDto settings,
        CancellationToken cancellationToken)
    {
        return Ok(await profileService.UpdateNotificationSettingsAsync(settings, cancellationToken));
    }
}
