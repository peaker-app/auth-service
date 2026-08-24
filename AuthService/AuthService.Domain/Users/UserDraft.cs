namespace AuthService.Domain.Users;

public sealed record UserDraft(
    Email Email,
    Username Username,
    string PasswordHash,
    TermsAcceptance AcceptedTerms);
