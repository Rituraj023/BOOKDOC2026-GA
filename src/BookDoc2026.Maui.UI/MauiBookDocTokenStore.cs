using BookDoc2026.Client;
using Microsoft.Maui.Storage;

namespace BookDoc2026.Maui.UI;

public sealed class MauiBookDocTokenStore : IBookDocTokenStore
{
    private const string RefreshTokenKey = "bookdoc.auth.refresh-token";
    private string? _accessToken;

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_accessToken);
    }

    public async ValueTask<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await SecureStorage.Default.GetAsync(RefreshTokenKey);
    }

    public async ValueTask SetTokensAsync(
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _accessToken = accessToken;
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _accessToken = null;
        SecureStorage.Default.Remove(RefreshTokenKey);
        return ValueTask.CompletedTask;
    }
}
