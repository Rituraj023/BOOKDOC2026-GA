namespace BookDoc2026.Client;

public interface IBookDocTokenStore
{
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    ValueTask<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);

    ValueTask SetTokensAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class InMemoryBookDocTokenStore : IBookDocTokenStore
{
    private string? _accessToken;
    private string? _refreshToken;

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_accessToken);

    public ValueTask<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_refreshToken);

    public ValueTask SetTokensAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _accessToken = null;
        _refreshToken = null;
        return ValueTask.CompletedTask;
    }
}
