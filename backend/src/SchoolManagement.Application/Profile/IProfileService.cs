namespace SchoolManagement.Application.Profile;

/// <summary>The logged-in user's own profile and settings. Every user may use it, for herself only.</summary>
public interface IProfileService
{
    Task<ProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Changes the user's own password after checking the current one.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<NotificationSettingsDto> GetNotificationSettingsAsync(CancellationToken cancellationToken = default);

    Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(
        NotificationSettingsDto settings,
        CancellationToken cancellationToken = default);
}
