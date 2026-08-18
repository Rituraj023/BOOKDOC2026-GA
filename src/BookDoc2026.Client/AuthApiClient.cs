using System.Net.Http.Json;
using System.Net.Http.Headers;
using BookDoc2026.Contracts.Auth;

namespace BookDoc2026.Client;

public sealed class AuthApiClient(HttpClient httpClient, IBookDocTokenStore tokenStore)
{
    public async Task<AuthSessionResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/v1/auth/login", request, cancellationToken);
        var session = await ApiResponseReader.ReadAsync<AuthSessionResponse>(response, cancellationToken);
        await tokenStore.SetTokensAsync(session.AccessToken, session.RefreshToken, cancellationToken);
        return session;
    }

    public async Task<AuthSessionResponse> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = await tokenStore.GetRefreshTokenAsync(cancellationToken)
            ?? throw new InvalidOperationException("No refresh token is available.");
        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/auth/refresh",
            new RefreshSessionRequest(refreshToken),
            cancellationToken);
        var session = await ApiResponseReader.ReadAsync<AuthSessionResponse>(response, cancellationToken);
        await tokenStore.SetTokensAsync(session.AccessToken, session.RefreshToken, cancellationToken);
        return session;
    }

    public async Task RevokeAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await tokenStore.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/revoke");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _ = await ApiResponseReader.ReadAsync<object>(response, cancellationToken);
                throw new InvalidOperationException("The session could not be revoked.");
            }
        }

        await tokenStore.ClearAsync(cancellationToken);
    }
}
