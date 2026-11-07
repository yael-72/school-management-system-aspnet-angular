using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Users;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Roles = RoleNames.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Lists staff members. Admin only.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<UserListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetUsers(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await userService.GetUsersAsync(query, cancellationToken));
    }
}
