using EmployeeRegistry.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeRegistry.Api.Modules.Auth;

public sealed record LoginRequest(string? Login, string? Password);

public sealed record LoginResponse(string Token, string Login, string Role, DateTimeOffset ExpiresAt);

public sealed record MeResponse(string Login, string Role);

public sealed class AuthService(AppDbContext db, PasswordHasher<Account> passwordHasher, JwtTokenService tokens)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var account = await db.Accounts.SingleOrDefaultAsync(
            candidate => candidate.Login == request.Login.Trim(),
            cancellationToken);

        if (account is null)
            return null;

        var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return null;

        var (token, expiresAt) = tokens.Create(account);
        return new LoginResponse(token, account.Login, account.Role, expiresAt);
    }
}
