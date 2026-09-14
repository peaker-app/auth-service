namespace AuthService.Application.Abstractions;

public interface IRefreshTokenPolicy
{
    TimeSpan RotationLeeway { get; }
}
