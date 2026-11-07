namespace SchoolManagement.Domain.Constants;

/// <summary>
/// Role names as stored in the Roles table. Used in [Authorize(Roles = ...)] and in JWT claims,
/// so keeping them in one place avoids typos in string literals.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
}
