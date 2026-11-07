using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.UnitTests.TestHelpers;

/// <summary>Plays the logged-in user in service tests (in the real app this comes from the JWT).</summary>
public class FakeCurrentUser(int id, string role) : ICurrentUser
{
    public int Id { get; } = id;
    public string Role { get; } = role;
    public bool IsAdmin => Role == RoleNames.Admin;

    public static FakeCurrentUser Admin(int id = 1) => new(id, RoleNames.Admin);

    public static FakeCurrentUser Teacher(int id) => new(id, RoleNames.Teacher);
}
