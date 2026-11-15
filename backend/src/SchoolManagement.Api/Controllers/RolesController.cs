using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Users;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Produces("application/json")]
[Authorize(Roles = RoleNames.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class RolesController(IUserService userService) : ControllerBase
{
    /// <summary>The roles a user can have (for the user form). Admin only.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await userService.GetRolesAsync(cancellationToken));
    }
}
