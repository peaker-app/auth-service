using FluentValidation;

namespace AuthService.Application.Maintenance.PurgeExpiredData;

internal sealed class PurgeExpiredDataCommandValidator : AbstractValidator<PurgeExpiredDataCommand>
{
    public PurgeExpiredDataCommandValidator() => RuleFor(command => command.UtcNow).NotEmpty();
}
