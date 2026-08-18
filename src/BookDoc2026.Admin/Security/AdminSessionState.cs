using BookDoc2026.Client;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.ErrorHandling;

namespace BookDoc2026.Admin.Security;

public sealed class AdminSessionState(TimeProvider timeProvider) : IBookDocTokenStore
{
    private string? _accessToken;
    private string? _refreshToken;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public event Action? Changed;
    public AuthSessionResponse? Session { get; private set; }
    public bool IsAuthenticated => Session is not null && !IsExpired;
    public bool IsExpired { get; private set; }
    public IReadOnlySet<string> Permissions { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

    public bool HasPermission(string permission) => IsAuthenticated && Permissions.Contains(permission);

    public async Task SignInAsync(
        AuthApiClient auth,
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await auth.LoginAsync(request, cancellationToken);
        Apply(session);
    }

    public async Task EnsureFreshAsync(
        AuthApiClient auth,
        CancellationToken cancellationToken = default)
    {
        if (Session is null || string.IsNullOrWhiteSpace(_refreshToken))
            throw new AdminSessionRequiredException("Sign in to continue.");
        if (Session.AccessTokenExpiresUtc > timeProvider.GetUtcNow().AddMinutes(1)) return;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (Session is null || string.IsNullOrWhiteSpace(_refreshToken))
                throw new AdminSessionRequiredException("Sign in to continue.");
            if (Session.AccessTokenExpiresUtc > timeProvider.GetUtcNow().AddMinutes(1)) return;
            try
            {
                Apply(await auth.RefreshAsync(cancellationToken));
            }
            catch (RemoteErrorException exception) when (
                exception.Error.Category is ErrorCategory.Authentication or ErrorCategory.Authorization)
            {
                await ClearAsync(cancellationToken);
                IsExpired = true;
                Changed?.Invoke();
                throw new AdminSessionExpiredException("Your session expired. Sign in again.");
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task SignOutAsync(AuthApiClient auth, CancellationToken cancellationToken = default)
    {
        try { await auth.RevokeAsync(cancellationToken); }
        finally { await ClearAsync(cancellationToken); }
    }

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_accessToken);

    public ValueTask<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_refreshToken);

    public ValueTask SetTokensAsync(
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _accessToken = null;
        _refreshToken = null;
        Session = null;
        Permissions = new HashSet<string>(StringComparer.Ordinal);
        IsExpired = false;
        Changed?.Invoke();
        return ValueTask.CompletedTask;
    }

    private void Apply(AuthSessionResponse session)
    {
        Session = session;
        Permissions = session.Permissions.ToHashSet(StringComparer.Ordinal);
        IsExpired = false;
        Changed?.Invoke();
    }
}

public sealed class AdminSessionRequiredException(string message) : InvalidOperationException(message);

public sealed class AdminSessionExpiredException(string message) : InvalidOperationException(message);
