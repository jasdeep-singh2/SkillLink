using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SkillLink.Api.Data;
using SkillLink.Api.Features.Auth;
using SkillLink.Api.Features.Users;
using SkillLink.Api.Tests.Infrastructure;

namespace SkillLink.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(SkillLinkApiFactory factory)
{
    private readonly HttpClient _anonymous = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_creates_user()
    {
        var email = $"new.{Guid.NewGuid():N}@example.test";

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "New User", "Optimizely dev", TestUsers.Password), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(Ct);
        Assert.Equal(email, user!.Email);
        Assert.Equal($"/api/users/{user.Id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Register_with_invalid_input_returns_400_with_an_error_per_field()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("not-an-email", "", null, "short"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Ct);
        var fields = problem!.Errors.Keys.Select(k => k.ToLowerInvariant()).ToArray();
        Assert.Contains("email", fields);
        Assert.Contains("displayname", fields);
        Assert.Contains("password", fields);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_409_problem()
    {
        var existing = await TestUsers.CreateAsync(factory);

        // Different casing: the citext column treats it as the same email.
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(existing.Email.ToUpperInvariant(), "Copycat", null, TestUsers.Password), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_401()
    {
        var user = await TestUsers.CreateAsync(factory);

        var wrongPassword = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, "wrong-password"), Ct);
        var unknownEmail = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@example.test", TestUsers.Password), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        // Same body too: nothing that tells an attacker which emails exist.
        Assert.Equal(Normalize(await wrongPassword.Content.ReadAsStringAsync(Ct)),
                     Normalize(await unknownEmail.Content.ReadAsStringAsync(Ct)));
    }

    [Fact]
    public async Task Me_returns_the_user_from_the_token()
    {
        var user = await TestUsers.CreateAsync(factory, "Alice");

        var me = await user.Client.GetFromJsonAsync<UserResponse>("/api/users/me", Ct);

        Assert.Equal(user.Id, me!.Id);
    }

    [Fact]
    public async Task Me_without_token_returns_401_problem()
    {
        var response = await _anonymous.GetAsync("/api/users/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem!.Status);
    }

    [Fact]
    public async Task Tampered_token_is_rejected()
    {
        var user = await TestUsers.CreateAsync(factory);
        var parts = user.Token.Split('.');
        // Swap in another user's payload but keep the original signature.
        var other = await TestUsers.CreateAsync(factory);
        var forged = $"{parts[0]}.{other.Token.Split('.')[1]}.{parts[2]}";

        var response = await SendWithToken(HttpMethod.Get, "/api/users/me", forged);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        // Issue a token "20 minutes ago": it lived 15 minutes, so it's 5 minutes past expiry (beyond the 30s clock skew).
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        var pastClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-20));
        var user = await TestUsers.CreateAsync(factory);
        var expired = new TokenService(options, pastClock)
            .CreateAccessToken(new User { Id = user.Id, Email = user.Email, DisplayName = "User" })
            .AccessToken;

        var response = await SendWithToken(HttpMethod.Get, "/api/users/me", expired);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendWithToken(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _anonymous.SendAsync(request, Ct);
    }

    // traceId differs per request, so drop it before comparing bodies.
    private static string Normalize(string body) =>
        System.Text.RegularExpressions.Regex.Replace(body, "\"traceId\":\"[^\"]*\"", "");
}
