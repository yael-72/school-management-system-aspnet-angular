using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Users;
using SchoolManagement.Domain.Constants;

namespace SchoolManagement.Api.Controllers;

/// <summary>Staff management. Admin only. (Every user edits her own profile through /api/users/me.)</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Roles = RoleNames.Admin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Lists staff members.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<UserListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetUsers(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await userService.GetUsersAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int id, CancellationToken cancellationToken)
    {
        return Ok(await userService.GetUserAsync(id, cancellationToken));
    }

    /// <summary>Adds a staff member with a first password (shared with them by the admin).</summary>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
    }

    /// <summary>Updates name, email, phone and role (not the password).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await userService.UpdateUserAsync(id, request, cancellationToken));
    }

    /// <summary>Activates or deactivates a user. A deactivated user cannot log in.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SetStatus(int id, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await userService.SetStatusAsync(id, request.IsActive!.Value, cancellationToken));
    }

    /// <summary>Deletes a user without school history; others must be deactivated instead.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await userService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }
}
