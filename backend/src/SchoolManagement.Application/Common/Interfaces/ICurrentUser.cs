namespace SchoolManagement.Application.Common.Interfaces;

/// <summary>
/// The user who sent the current request, read from the JWT claims.
/// </summary>
public interface ICurrentUser
{
    int Id { get; }
    string Role { get; }
    bool IsAdmin { get; }
}
