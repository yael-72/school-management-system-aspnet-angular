using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Infrastructure.Identity;

namespace SchoolManagement.Api.Infrastructure;

/// <summary>
/// Reads the logged-in user from the claims of the validated JWT in the current HTTP request.
/// </summary>
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("There is no current HTTP request.");

    public int Id => int.Parse(
        Principal.FindFirst(AuthClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("The request has no authenticated user."));

    public string Role => Principal.FindFirst(AuthClaimTypes.Role)?.Value ?? string.Empty;

    public bool IsAdmin => Role == RoleNames.Admin;
}
