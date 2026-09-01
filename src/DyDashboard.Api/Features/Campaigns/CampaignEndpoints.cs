using DyDashboard.Api.Common.Validation;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace DyDashboard.Api.Features.Campaigns;

/// <summary>
/// HTTP layer for campaigns. Endpoints only translate between HTTP and the
/// service: read validated input, call the service, shape the response. No
/// business logic lives here.
/// </summary>
public static class CampaignEndpoints
{
    public static RouteGroupBuilder MapCampaignEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync).ValidateWith<ListCampaignsQuery>();
        group.MapPost("/", CreateAsync).ValidateWith<CreateCampaignRequest>();
        group.MapGet("/{id}", GetOneAsync);
        group.MapPatch("/{id}", UpdateAsync).ValidateWith<UpdateCampaignRequest>();
        group.MapDelete("/{id}", DeleteAsync);
        return group;
    }

    // GET / — paginated, filterable, sortable list. The page of resources is
    // returned as a bare array; pagination metadata travels in response headers.
    private static async Task<IResult> ListAsync(
        [AsParameters] ListCampaignsQuery query, CampaignService service,
        HttpContext http, CancellationToken ct)
    {
        var (data, pagination) = await service.ListAsync(query, ct);

        http.Response.Headers["X-Total-Count"] = pagination.Total.ToString();
        http.Response.Headers["X-Total-Pages"] = pagination.TotalPages.ToString();
        http.Response.Headers["X-Page"] = pagination.Page.ToString();
        http.Response.Headers["X-Limit"] = pagination.Limit.ToString();
        http.Response.Headers["Link"] = BuildLinkHeader(http, query, pagination);

        return Results.Ok(data);
    }

    // GET /{id} — a single resource.
    private static async Task<IResult> GetOneAsync(
        string id, CampaignService service, CancellationToken ct) =>
        Results.Ok(await service.GetAsync(id, ct));

    // POST / — create a resource; responds 201 with a Location header.
    private static async Task<IResult> CreateAsync(
        [FromBody] CreateCampaignRequest body, CampaignService service,
        HttpContext http, CancellationToken ct)
    {
        var campaign = await service.CreateAsync(body, ct);
        var basePath = http.Request.Path.Value!.TrimEnd('/');
        return Results.Created($"{basePath}/{campaign.Id}", campaign);
    }

    // PATCH /{id} — partial update.
    private static async Task<IResult> UpdateAsync(
        string id, [FromBody] UpdateCampaignRequest body, CampaignService service,
        CancellationToken ct) =>
        Results.Ok(await service.UpdateAsync(id, body, ct));

    // DELETE /{id} — remove a resource; 204 No Content on success.
    private static async Task<IResult> DeleteAsync(
        string id, CampaignService service, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Results.NoContent();
    }

    // Build an RFC 5988 Link header (first/prev/next/last) for the list endpoint.
    private static string BuildLinkHeader(
        HttpContext http, ListCampaignsQuery query, PaginationMeta pagination)
    {
        var basePath =
            $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}{http.Request.Path}"
                .TrimEnd('/');

        string LinkFor(int page, string rel)
        {
            var qs = QueryString.Empty
                .Add("page", page.ToString())
                .Add("limit", pagination.Limit.ToString());
            if (query.Status is not null) qs = qs.Add("status", query.Status);
            if (query.Channel is not null) qs = qs.Add("channel", query.Channel);
            if (query.Search is not null) qs = qs.Add("search", query.Search);
            qs = qs.Add("sort", query.SortOrDefault).Add("order", query.OrderOrDefault);
            return $"<{basePath}{qs.Value}>; rel=\"{rel}\"";
        }

        var links = new List<string> { LinkFor(1, "first"), LinkFor(pagination.TotalPages, "last") };
        if (pagination.Page > 1) links.Add(LinkFor(pagination.Page - 1, "prev"));
        if (pagination.Page < pagination.TotalPages) links.Add(LinkFor(pagination.Page + 1, "next"));
        return string.Join(", ", links);
    }
}
