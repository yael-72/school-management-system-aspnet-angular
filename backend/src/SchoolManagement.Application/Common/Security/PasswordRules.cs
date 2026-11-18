namespace SchoolManagement.Application.Common.Security;

/// <summary>
/// The one password rule of the app, used by every request that sets a password
/// (new user, password change, admin reset). The Angular forms use the same pattern.
/// </summary>
public static class PasswordRules
{
    /// <summary>8–100 characters with at least one letter and one digit.</summary>
    public const string Pattern = @"^(?=.*[A-Za-z])(?=.*\d).{8,100}$";

    public const string ErrorMessage = "Password must be 8–100 characters and contain a letter and a digit.";
}
