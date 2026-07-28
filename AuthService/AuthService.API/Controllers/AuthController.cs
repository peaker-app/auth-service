using AuthService.API.Requests;
using AuthService.Application.Authentication;
using AuthService.Application.EmailConfirmations.ResendEmailConfirmation;
using AuthService.Application.Users.DeleteAccount;
using Common.API.Results;
using Common.Application.Abstractions;
using Common.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender, IUserContext userContext) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        Result<Guid> result = await sender.Send(request.ToCommand(), cancellationToken);

        return result.ToActionResult(id => StatusCode(StatusCodes.Status201Created, new { id }));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        Result<AuthTokensResponse> result = await sender.Send(request.ToCommand(ClientIpAddress), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        Result<AuthTokensResponse> result = await sender.Send(request.ToCommand(ClientIpAddress), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("email/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("email/resend")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ResendEmailConfirmation(CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new ResendEmailConfirmationCommand(userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        Result result = await sender.Send(new DeleteAccountCommand(userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    private string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
