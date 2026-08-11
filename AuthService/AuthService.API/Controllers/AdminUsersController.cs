using AuthService.API.Requests;
using AuthService.Application.Users.LockUser;
using AuthService.Application.Users.RevokeRole;
using AuthService.Application.Users.UnlockUser;
using AuthService.Domain.Users;
using Common.API.Results;
using Common.API.Security;
using Common.Application.Abstractions;
using Common.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationExtensions.AdminPolicyName)]
public sealed class AdminUsersController(ISender sender, IUserContext userContext) : ControllerBase
{
    [HttpPost("{userId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Lock(Guid userId, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new LockUserCommand(userContext.UserId, userId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{userId:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unlock(Guid userId, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new UnlockUserCommand(userContext.UserId, userId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GrantRole(
        Guid userId,
        GrantRoleRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(userContext.UserId, userId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{userId:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeRole(
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new RevokeRoleCommand(userContext.UserId, userId, role), cancellationToken);

        return result.ToActionResult();
    }
}
