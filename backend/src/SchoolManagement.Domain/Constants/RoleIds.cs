namespace SchoolManagement.Domain.Constants;

/// <summary>
/// Fixed primary keys for the seeded roles. Roles are reference data that never change at runtime,
/// so their ids are stable and can be used directly in code and in seed data.
/// </summary>
public static class RoleIds
{
    public const int Admin = 1;
    public const int Teacher = 2;
}
