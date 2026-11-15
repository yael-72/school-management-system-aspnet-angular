using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Auth;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Logs in with email and password and returns a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }

    /// <summary>Returns the logged-in user.</summary>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetCurrentUserAsync(currentUser.Id, cancellationToken));
    }
}
