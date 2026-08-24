namespace AuthService.Application.Abstractions;

public sealed record JsonWebKeySetResponse(IReadOnlyCollection<JsonWebKeyResponse> Keys);
