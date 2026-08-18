using System.Net;
using System.Net.Http.Json;
using BookDoc2026.Admin.Security;
using BookDoc2026.Client;
using BookDoc2026.Contracts.Auth;
using BookDoc2026.Contracts.Common;
using BookDoc2026.Contracts.Security;
using BookDoc2026.ErrorHandling;

namespace BookDoc2026.UnitTests;

public sealed class AdminSecurityTests
{
    [Fact]
    public void NetworkPolicy_FailsClosedWhenAllowListIsEmpty()
    {
        var policy = CreateNetworkPolicy([], []);

        var result = policy.Evaluate(IPAddress.Loopback, null);

        Assert.False(result.IsAllowed);
        Assert.Equal("allow_list_empty", result.Reason);
    }

    [Fact]
    public void NetworkPolicy_AllowsDirectClientInsideConfiguredCidr()
    {
        var policy = CreateNetworkPolicy(["10.20.0.0/16"], []);

        var result = policy.Evaluate(IPAddress.Parse("10.20.4.25"), null);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void NetworkPolicy_DoesNotTrustForwardedHeaderFromUntrustedPeer()
    {
        var policy = CreateNetworkPolicy(["10.20.0.0/16"], ["192.168.50.0/24"]);

        var result = policy.Evaluate(IPAddress.Parse("203.0.113.10"), "10.20.4.25");

        Assert.False(result.IsAllowed);
        Assert.Equal("client_not_allowed", result.Reason);
    }

    [Fact]
    public void NetworkPolicy_ResolvesClientThroughTrustedProxyChain()
    {
        var policy = CreateNetworkPolicy(["10.20.0.0/16"], ["192.168.50.0/24"]);

        var result = policy.Evaluate(
            IPAddress.Parse("192.168.50.10"),
            "10.20.4.25, 192.168.50.11");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void NetworkPolicy_RejectsMalformedConfigurationWithoutLeakingItsValue()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreateNetworkPolicy(["not-a-network"], []));

        Assert.DoesNotContain("not-a-network", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionPolicy_RequiresACommunicationPermission()
    {
        Assert.False(AdminPermissionPolicy.CanAccessCommunications([FoundationPermissions.PatientsView]));
        Assert.True(AdminPermissionPolicy.CanAccessCommunications([FoundationPermissions.MessageTemplatesManage]));
    }

    [Fact]
    public async Task SessionState_SignsInAndKeepsRotatedTokensInServerStore()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 18, 8, 0, 0, TimeSpan.Zero));
        var state = new AdminSessionState(clock);
        var handler = new StubHttpHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/auth/login" => Envelope(Session("access-1", "refresh-1", clock.GetUtcNow().AddMinutes(2),
                FoundationPermissions.MessageTemplatesView)),
            "/api/v1/auth/refresh" => Envelope(Session("access-2", "refresh-2", clock.GetUtcNow().AddMinutes(15),
                FoundationPermissions.MessageTemplatesPublish)),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var auth = CreateAuthClient(handler, state);

        await state.SignInAsync(auth, new LoginRequest("admin@example.test", "secret"));
        clock.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));
        await state.EnsureFreshAsync(auth);

        Assert.True(state.IsAuthenticated);
        Assert.Equal("access-2", await state.GetAccessTokenAsync());
        Assert.Equal("refresh-2", await state.GetRefreshTokenAsync());
        Assert.True(state.HasPermission(FoundationPermissions.MessageTemplatesPublish));
        Assert.False(state.HasPermission(FoundationPermissions.MessageTemplatesView));
        Assert.Equal(1, handler.RefreshCalls);
    }

    [Fact]
    public async Task SessionState_ExpiresAndClearsTokensWhenRefreshIsRejected()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 18, 8, 0, 0, TimeSpan.Zero));
        var state = new AdminSessionState(clock);
        var handler = new StubHttpHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/auth/login" => Envelope(Session("access-1", "refresh-1", clock.GetUtcNow().AddSeconds(30),
                FoundationPermissions.MessageTemplatesView)),
            "/api/v1/auth/refresh" => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = JsonContent.Create(new ApiError(
                    ErrorCodes.Unauthorized,
                    "Sign in again.",
                    "test-trace",
                    ErrorCategory.Authentication))
            },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var auth = CreateAuthClient(handler, state);
        await state.SignInAsync(auth, new LoginRequest("admin@example.test", "secret"));

        await Assert.ThrowsAsync<AdminSessionExpiredException>(() => state.EnsureFreshAsync(auth));

        Assert.True(state.IsExpired);
        Assert.False(state.IsAuthenticated);
        Assert.Null(await state.GetAccessTokenAsync());
        Assert.Null(await state.GetRefreshTokenAsync());
    }

    [Fact]
    public async Task SessionState_SignOutRevokesBearerAndClearsLocalSession()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 18, 8, 0, 0, TimeSpan.Zero));
        var state = new AdminSessionState(clock);
        var handler = new StubHttpHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/auth/login" => Envelope(Session("access-1", "refresh-1", clock.GetUtcNow().AddMinutes(15),
                FoundationPermissions.MessageTemplatesView)),
            "/api/v1/auth/revoke" => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        var auth = CreateAuthClient(handler, state);
        await state.SignInAsync(auth, new LoginRequest("admin@example.test", "secret"));

        await state.SignOutAsync(auth);

        Assert.Equal("Bearer", handler.RevokeAuthorization?.Scheme);
        Assert.Equal("access-1", handler.RevokeAuthorization?.Parameter);
        Assert.False(state.IsAuthenticated);
        Assert.Null(await state.GetRefreshTokenAsync());
    }

    private static AdminNetworkPolicy CreateNetworkPolicy(string[] allowed, string[] trusted) =>
        new(new AdminNetworkOptions
        {
            Enforce = true,
            AllowedNetworks = allowed,
            TrustedProxies = trusted
        });

    private static AuthApiClient CreateAuthClient(HttpMessageHandler handler, IBookDocTokenStore store) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://bookdoc.test") }, store);

    private static AuthSessionResponse Session(
        string access,
        string refresh,
        DateTimeOffset expiresUtc,
        params string[] permissions) =>
        new(
            access,
            refresh,
            expiresUtc,
            "Admin User",
            "admin@example.test",
            true,
            null,
            ["PlatformOperator"],
            permissions);

    private static HttpResponseMessage Envelope(AuthSessionResponse session) =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ApiEnvelope<AuthSessionResponse>(session, "test-trace"))
        };

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public int RefreshCalls { get; private set; }
        public System.Net.Http.Headers.AuthenticationHeaderValue? RevokeAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/auth/refresh") RefreshCalls++;
            if (request.RequestUri.AbsolutePath == "/api/v1/auth/revoke")
                RevokeAuthorization = request.Headers.Authorization;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
