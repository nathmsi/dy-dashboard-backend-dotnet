using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DyDashboard.Api.Tests;

// Integration tests exercise the real API against an in-memory SQLite database,
// mirroring the Node supertest suite endpoint-for-endpoint.
public class CampaignsIntegrationTests(CampaignsApiFactory factory) : IClassFixture<CampaignsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly object ValidCampaign = new
    {
        name = "Spring Sale",
        status = "active",
        channel = "Web",
        conversionRate = 5.2,
        visitors = 1000,
        startDate = "2026-04-01",
    };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res) =>
        JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Health_returns_ok()
    {
        var res = await _client.GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(res)).GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task Post_creates_a_campaign_with_201_and_location_header()
    {
        var res = await _client.PostAsJsonAsync("/api/v1/campaigns", ValidCampaign);
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await JsonAsync(res);
        var id = body.GetProperty("id").GetString();
        id.Should().StartWith("camp-");
        body.GetProperty("createdAt").GetString().Should().NotBeNullOrEmpty();
        res.Headers.Location!.ToString().Should().Be($"/api/v1/campaigns/{id}");
    }

    [Fact]
    public async Task Post_rejects_an_invalid_payload_with_422()
    {
        var res = await _client.PostAsJsonAsync("/api/v1/campaigns", new { name = "x" });
        res.StatusCode.Should().Be((HttpStatusCode)422);
        (await JsonAsync(res)).GetProperty("error").GetProperty("code").GetString()
            .Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Post_rejects_a_malformed_json_body_with_400()
    {
        var content = new StringContent("{ not json", Encoding.UTF8, "application/json");
        var res = await _client.PostAsync("/api/v1/campaigns", content);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await JsonAsync(res)).GetProperty("error").GetProperty("code").GetString()
            .Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task Get_returns_a_paginated_array_with_pagination_headers()
    {
        var res = await _client.GetAsync("/api/v1/campaigns?limit=5");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await JsonAsync(res);
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().BeLessThanOrEqualTo(5);
        res.Headers.Contains("X-Total-Count").Should().BeTrue();
        res.Headers.GetValues("Link").First().Should().Contain("rel=\"first\"");
    }

    [Fact]
    public async Task Get_filters_by_status()
    {
        await _client.PostAsJsonAsync("/api/v1/campaigns", ValidCampaign);
        var res = await _client.GetAsync("/api/v1/campaigns?status=active");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        foreach (var item in (await JsonAsync(res)).EnumerateArray())
            item.GetProperty("status").GetString().Should().Be("active");
    }

    [Fact]
    public async Task Get_rejects_an_invalid_query_parameter_with_422()
    {
        var res = await _client.GetAsync("/api/v1/campaigns?status=bogus");
        res.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task Fetch_update_and_delete_a_campaign()
    {
        var created = await _client.PostAsJsonAsync("/api/v1/campaigns", ValidCampaign);
        var id = (await JsonAsync(created)).GetProperty("id").GetString();

        var fetched = await _client.GetAsync($"/api/v1/campaigns/{id}");
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(fetched)).GetProperty("name").GetString().Should().Be("Spring Sale");

        var updated = await _client.PatchAsJsonAsync($"/api/v1/campaigns/{id}", new { status = "paused" });
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(updated)).GetProperty("status").GetString().Should().Be("paused");

        var deleted = await _client.DeleteAsync($"/api/v1/campaigns/{id}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var missing = await _client.GetAsync($"/api/v1/campaigns/{id}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await JsonAsync(missing)).GetProperty("error").GetProperty("code").GetString()
            .Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Get_returns_404_for_an_unknown_id()
    {
        var res = await _client.GetAsync("/api/v1/campaigns/does-not-exist");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Legacy_alias_still_responds()
    {
        var res = await _client.GetAsync("/api/campaigns");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(res)).ValueKind.Should().Be(JsonValueKind.Array);
    }
}
