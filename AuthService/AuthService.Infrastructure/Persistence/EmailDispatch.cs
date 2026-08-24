namespace AuthService.Infrastructure.Persistence;

public sealed class EmailDispatch
{
    public Guid Id { get; init; }

    public string RecipientHash { get; init; } = null!;

    public DateTime SentAtUtc { get; init; }
}
