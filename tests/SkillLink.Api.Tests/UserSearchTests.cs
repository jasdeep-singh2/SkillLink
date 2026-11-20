using System.Net;
using System.Net.Http.Json;
using SkillLink.Api.Features.Users;
using SkillLink.Api.Tests.Infrastructure;

namespace SkillLink.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class UserSearchTests(SkillLinkApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_matches_part_of_the_name_ignoring_case_and_excludes_me()
    {
        // A unique name so other tests' users (same database) can't match.
        var unique = $"Zed{Guid.NewGuid():N}"[..12];
        var me = await TestUsers.CreateAsync(factory, unique + "Me");
        var other = await TestUsers.CreateAsync(factory, unique + "Other");

        var results = await Search(me, unique.ToLowerInvariant());

        var found = Assert.Single(results);
        Assert.Equal(other.Id, found.Id);
    }

    [Fact]
    public async Task Search_results_do_not_expose_email_addresses()
    {
        var unique = $"Zed{Guid.NewGuid():N}"[..12];
        var me = await TestUsers.CreateAsync(factory);
        await TestUsers.CreateAsync(factory, unique);

        var json = await me.Client.GetStringAsync($"/api/users/search?q={unique}", Ct);

        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Wildcard_characters_are_treated_as_plain_text()
    {
        var me = await TestUsers.CreateAsync(factory);

        // Without escaping, "%%" would match every user in the database.
        var results = await Search(me, "%%");

        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_needs_at_least_two_characters()
    {
        var me = await TestUsers.CreateAsync(factory);

        var response = await me.Client.GetAsync("/api/users/search?q=a", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<List<UserSummary>> Search(TestUser user, string q) =>
        (await user.Client.GetFromJsonAsync<List<UserSummary>>($"/api/users/search?q={Uri.EscapeDataString(q)}", Ct))!;
}
