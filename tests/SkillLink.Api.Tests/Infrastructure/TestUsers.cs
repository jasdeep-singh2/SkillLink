using System.Net.Http.Headers;
using System.Net.Http.Json;
using SkillLink.Api.Features.Auth;
using SkillLink.Api.Features.Users;

namespace SkillLink.Api.Tests.Infrastructure;

public sealed record TestUser(Guid Id, string Email, string Token, HttpClient Client);

public static class TestUsers
{
    public const string Password = "Passw0rd!123";

    // Every test creates its own users with unique emails, so tests never depend on each other's data.
    public static async Task<TestUser> CreateAsync(SkillLinkApiFactory factory, string name = "User")
    {
        var anonymous = factory.CreateClient();
        var email = $"{name.ToLowerInvariant()}.{Guid.NewGuid():N}@example.test";

        var registered = await anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, name, null, Password));
        registered.EnsureSuccessStatusCode();
        var user = (await registered.Content.ReadFromJsonAsync<UserResponse>())!;

        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new TestUser(user.Id, email, token, client);
    }
}
