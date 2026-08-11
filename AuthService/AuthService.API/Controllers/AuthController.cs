using AuthService.API.Requests;
using AuthService.Application.Authentication;
using AuthService.Application.EmailConfirmations.ResendEmailConfirmation;
using AuthService.Application.Users.ExportMyData;
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
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(), cancellationToken);

        return result.ToActionResult(() => StatusCode(StatusCodes.Status202Accepted));
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

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(), cancellationToken);

        return result.ToActionResult(() => StatusCode(StatusCodes.Status202Accepted));
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(), cancellationToken);

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

    [HttpGet("me/export")]
    [Authorize]
    [ProducesResponseType(typeof(AccountExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportMyData(CancellationToken cancellationToken)
    {
        Result<AccountExportResponse> result = await sender.Send(
            new ExportMyDataQuery(userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    private string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
