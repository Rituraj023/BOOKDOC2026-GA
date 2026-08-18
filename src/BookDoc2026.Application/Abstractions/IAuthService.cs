using BookDoc2026.Contracts.Auth;

namespace BookDoc2026.Application.Abstractions;

public interface IAuthService
{
    Task<AuthSessionResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthSessionResponse> RefreshAsync(RefreshSessionRequest request, CancellationToken cancellationToken);

    Task RevokeAsync(uint userId, CancellationToken cancellationToken);
}
