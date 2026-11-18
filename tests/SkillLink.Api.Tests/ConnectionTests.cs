using System.Net;
using System.Net.Http.Json;
using SkillLink.Api.Features.Connections;
using SkillLink.Api.Tests.Infrastructure;

namespace SkillLink.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class ConnectionTests(SkillLinkApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Request_then_accept_makes_both_users_connected()
    {
        var alice = await TestUsers.CreateAsync(factory, "Alice");
        var bob = await TestUsers.CreateAsync(factory, "Bob");

        var sent = await Send(alice, bob.Id);
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);

        // Alice can't accept her own request: only the addressee can.
        var aliceAccepts = await alice.Client.PostAsync($"/api/connections/{alice.Id}/accept", null, Ct);
        Assert.Equal(HttpStatusCode.NotFound, aliceAccepts.StatusCode);

        var bobAccepts = await bob.Client.PostAsync($"/api/connections/{alice.Id}/accept", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, bobAccepts.StatusCode);

        Assert.Equal([bob.Id], (await Connections(alice)).Select(c => c.UserId));
        Assert.Equal([alice.Id], (await Connections(bob)).Select(c => c.UserId));
    }

    [Fact]
    public async Task Connecting_with_yourself_returns_400()
    {
        var alice = await TestUsers.CreateAsync(factory);

        var response = await Send(alice, alice.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_in_the_opposite_direction_is_a_duplicate()
    {
        var alice = await TestUsers.CreateAsync(factory);
        var bob = await TestUsers.CreateAsync(factory);
        await Send(alice, bob.Id);

        // (Bob -> Alice) is the same pair as (Alice -> Bob): the pair uniqueness index rejects it.
        var reverse = await Send(bob, alice.Id);

        Assert.Equal(HttpStatusCode.Conflict, reverse.StatusCode);
    }

    [Fact]
    public async Task Simultaneous_requests_create_only_one_connection()
    {
        var alice = await TestUsers.CreateAsync(factory);
        var bob = await TestUsers.CreateAsync(factory);

        // The race a "check, then insert" approach would lose. Only the database constraint can win it.
        var results = await Task.WhenAll(Send(alice, bob.Id), Send(bob, alice.Id));

        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Request_to_unknown_user_returns_404()
    {
        var alice = await TestUsers.CreateAsync(factory);

        var response = await Send(alice, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Suggestions_include_friends_of_friends_with_mutual_count()
    {
        var alice = await TestUsers.CreateAsync(factory, "Alice");
        var bob = await TestUsers.CreateAsync(factory, "Bob");
        var carol = await TestUsers.CreateAsync(factory, "Carol");
        await Connect(alice, bob);
        await Connect(bob, carol);

        var suggestions = await alice.Client.GetFromJsonAsync<List<SuggestionResponse>>("/api/me/suggestions", Ct);

        var carolSuggestion = Assert.Single(suggestions!);
        Assert.Equal(carol.Id, carolSuggestion.Id);
        Assert.Equal(1, carolSuggestion.MutualCount);
    }

    [Fact]
    public async Task Suggestions_are_rate_limited_per_user()
    {
        var greedy = await TestUsers.CreateAsync(factory);
        var polite = await TestUsers.CreateAsync(factory);

        // 20 at once against a bucket of 10: some must be rejected.
        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => greedy.Client.GetAsync("/api/me/suggestions", Ct)));

        var rejected = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToList();
        Assert.NotEmpty(rejected);
        Assert.NotNull(rejected[0].Headers.RetryAfter);
        Assert.Equal("application/problem+json", rejected[0].Content.Headers.ContentType?.MediaType);

        // Another user has their own bucket, so they're unaffected.
        var politeResponse = await polite.Client.GetAsync("/api/me/suggestions", Ct);
        Assert.Equal(HttpStatusCode.OK, politeResponse.StatusCode);
    }

    private static Task<HttpResponseMessage> Send(TestUser from, Guid to) =>
        from.Client.PostAsJsonAsync("/api/connections", new SendConnectionRequest(to), Ct);

    private static async Task Connect(TestUser from, TestUser to)
    {
        (await Send(from, to.Id)).EnsureSuccessStatusCode();
        (await to.Client.PostAsync($"/api/connections/{from.Id}/accept", null, Ct)).EnsureSuccessStatusCode();
    }

    private static async Task<List<ConnectionResponse>> Connections(TestUser user) =>
        (await user.Client.GetFromJsonAsync<List<ConnectionResponse>>("/api/me/connections", Ct))!;
}
